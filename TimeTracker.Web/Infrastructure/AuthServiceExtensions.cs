using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using TimeTracker.Web.Data;
using TimeTracker.Web.Features.Auth;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Infrastructure;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddApplicationAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<User, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<IdentityDataContext>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>();

        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.FromMinutes(
                configuration.GetValue<int>("Authentication:SecurityStampValidationIntervalMinutes", 30));
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.LoginPath = "/login";
            options.ExpireTimeSpan = TimeSpan.FromDays(1);
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
                options.SignInScheme = IdentityConstants.ExternalScheme;
            });

        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
        });

        return services;
    }
}
