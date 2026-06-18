using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace TimeTracker.Web.Infrastructure;

public class DatabaseWarmupMiddleware(RequestDelegate next)
{
    private static readonly string WarmupHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Waking up — TimeTracker</title>
        <style>
          *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
          body {
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
            background: #f8fafc;
            color: #334155;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100dvh;
            padding: 2rem;
          }
          .card {
            max-width: 480px;
            text-align: center;
            background: #fff;
            border-radius: 12px;
            padding: 2.5rem 2rem;
            box-shadow: 0 1px 3px rgba(0,0,0,0.08), 0 4px 12px rgba(0,0,0,0.04);
          }
          .loader {
            width: 40px;
            height: 40px;
            border: 4px solid #e2e8f0;
            border-top-color: #6366f1;
            border-radius: 50%;
            animation: spin 0.8s linear infinite;
            margin: 0 auto 1.5rem;
          }
          @keyframes spin { to { transform: rotate(360deg); } }
          h1 { font-size: 1.25rem; font-weight: 600; margin-bottom: 0.5rem; }
          p { font-size: 0.95rem; line-height: 1.5; color: #64748b; }
          .note { margin-top: 1.25rem; font-size: 0.825rem; color: #94a3b8; }
        </style>
        </head>
        <body>
        <div class="card">
          <div class="loader"></div>
          <h1>TimeTracker is waking up</h1>
          <p>The database has been idle and is restarting. This usually takes less than a minute.</p>
          <p class="note">Please try again in a moment.</p>
        </div>
        </body>
        </html>
        """;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (IsDatabaseUnavailable(ex))
        {
            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = 503;

            if (IsApiRequest(context))
            {
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync("""
                    {"status":503,"title":"The database is waking up from idle. Please try again in a few seconds.","type":"https://httpstatuses.com/503"}
                    """);
            }
            else
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(WarmupHtml);
            }
        }
    }

    private static bool IsApiRequest(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

    private static bool IsDatabaseUnavailable(Exception ex)
    {
        var current = ex;
        while (current is not null)
        {
            if (current is SqlException sqlEx)
            {
                // Common SQL error numbers for connectivity / wake-up:
                // -2 = timeout, 2 = network-related, 53 = transport-level,
                // 4060 = cannot open database, 10054 = connection reset,
                // 10060 = connection timeout, 11001 = host unknown
                return sqlEx.Number switch
                {
                    -2 or 2 or 53 or 4060 or 10054 or 10060 or 11001 => true,
                    _ => false
                };
            }

            if (current is DbException)
                return true;

            if (current is InvalidOperationException ioe &&
                ioe.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
                return true;

            current = current.InnerException;
        }

        return false;
    }
}
