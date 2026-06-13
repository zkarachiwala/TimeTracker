using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TimeTracker.Web.Infrastructure;

public static class RateLimitingServiceExtensions
{
    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
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
