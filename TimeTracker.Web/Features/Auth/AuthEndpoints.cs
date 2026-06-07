using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using TimeTracker.Contracts.Auth;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/challenge", (string provider, string? returnUrl, SignInManager<User> signInManager) =>
        {
            var redirectUri = $"/auth/callback?returnUrl={Uri.EscapeDataString(returnUrl ?? "/timeentries")}";
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUri);
            return Results.Challenge(properties, [provider]);
        }).RequireRateLimiting("auth");

        app.MapGet("/auth/callback", async (
            SignInManager<User> signInManager,
            IExternalLoginService externalLoginService,
            string? returnUrl) =>
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info is null)
                return Results.Redirect("/login?error=external-login-failed");

            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Results.Redirect("/login?error=no-email");

            var result = await externalLoginService.FindOrCreateUserAsync(email, info.LoginProvider, info.ProviderKey);

            if (result.Status == ExternalLoginStatus.EmailNotAllowed)
                return Results.Redirect("/access-denied");

            if (result.Status != ExternalLoginStatus.Success)
                return Results.Redirect($"/login?error={result.Status.ToString().ToLowerInvariant().Replace("_", "-")}");

            await signInManager.SignInAsync(result.User!, isPersistent: true);
            var safeReturnUrl = Uri.TryCreate(returnUrl, UriKind.Relative, out _) ? returnUrl! : "/timeentries";
            return Results.LocalRedirect(safeReturnUrl);
        }).RequireRateLimiting("auth");

        app.MapPost("/auth/logout", async (SignInManager<User> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Redirect("/login");
        });

        app.MapGet("/api/auth/providers", async (IAuthenticationSchemeProvider schemeProvider) =>
        {
            var schemes = await schemeProvider.GetAllSchemesAsync();
            var external = schemes
                .Where(s => !string.IsNullOrEmpty(s.DisplayName))
                .Select(s => new { s.Name, s.DisplayName });
            return Results.Ok(external);
        });

        app.MapGet("/api/auth/user", (HttpContext ctx) =>
        {
            if (ctx.User.Identity?.IsAuthenticated != true)
                return Results.Ok(new UserInfoResponse(false, null, []));

            var email = ctx.User.Identity.Name;
            var roles = ctx.User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToArray();
            return Results.Ok(new UserInfoResponse(true, email, roles));
        }).RequireRateLimiting("auth");
    }
}
