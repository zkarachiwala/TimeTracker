using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TimeTracker.Web.Data;

namespace TimeTracker.Web.Infrastructure;

public static class HealthCheckServiceExtensions
{
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<TimeTrackerDataContext>("app-db")
            .AddDbContextCheck<IdentityDataContext>("identity-db");

        return services;
    }

    public static IEndpointRouteBuilder MapApplicationHealthChecks(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
                });
            }
        }).AllowAnonymous();

        return app;
    }
}
