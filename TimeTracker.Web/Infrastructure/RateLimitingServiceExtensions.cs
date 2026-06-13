using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TimeTracker.Web.Infrastructure;

public static class RateLimitingServiceExtensions
{
    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue<int>("RateLimiting:Global:PermitLimit", 120),
                        Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:Global:WindowMinutes", 1)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            options.AddFixedWindowLimiter("write", policy =>
            {
                policy.PermitLimit = configuration.GetValue<int>("RateLimiting:Write:PermitLimit", 60);
                policy.Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:Write:WindowMinutes", 1));
                policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                policy.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("all-entries", policy =>
            {
                policy.PermitLimit = configuration.GetValue<int>("RateLimiting:AllEntries:PermitLimit", 10);
                policy.Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:AllEntries:WindowMinutes", 1));
                policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                policy.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("auth", policy =>
            {
                policy.PermitLimit = configuration.GetValue<int>("RateLimiting:Auth:PermitLimit", 10);
                policy.Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:Auth:WindowMinutes", 1));
                policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                policy.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("auth-status", policy =>
            {
                policy.PermitLimit = configuration.GetValue<int>("RateLimiting:AuthStatus:PermitLimit", 10);
                policy.Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:AuthStatus:WindowMinutes", 1));
                policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                policy.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
