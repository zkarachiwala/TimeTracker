using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TimeTracker.Web.Data;

namespace TimeTracker.Web.Infrastructure;

public static class HealthCheckServiceExtensions
{
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<TimeTrackerDataContext>("app-db", tags: ["db"])
            .AddDbContextCheck<IdentityDataContext>("identity-db", tags: ["db"]);

        return services;
    }

    public static IEndpointRouteBuilder MapApplicationHealthChecks(this IEndpointRouteBuilder app)
    {
        // Liveness — no DB ping. Do NOT point an external monitor at this while on Azure F1.
        // A 5-min ping keeps the process alive 24/7 and exhausts the 60 CPU-min/day quota
        // even with no real user traffic. Re-enable external monitoring only after moving off F1.
        // Also: hitting the DB on every external check would prevent Azure SQL auto-pause
        // and exhaust the 100,000 free vCore-seconds in ~2 days.
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = async (context, _) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { status = "Healthy" });
            }
        }).AllowAnonymous();

        // Readiness — includes DB connectivity. Use manually to diagnose DB issues.
        // Not monitored externally to protect the Azure SQL free vCore-second allowance.
        app.MapHealthChecks("/health/detail", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("db"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
                });
            }
        }).RequireAuthorization();

        return app;
    }
}
