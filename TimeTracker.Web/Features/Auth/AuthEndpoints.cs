using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
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
        });

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
                return Results.Forbid();

            if (result.Status != ExternalLoginStatus.Success)
                return Results.Redirect($"/login?error={result.Status.ToString().ToLowerInvariant().Replace("_", "-")}");

            await signInManager.SignInAsync(result.User!, isPersistent: true);
            var safeReturnUrl = Uri.TryCreate(returnUrl, UriKind.Relative, out _) ? returnUrl! : "/timeentries";
            return Results.LocalRedirect(safeReturnUrl);
        });

        app.MapPost("/auth/logout", async (SignInManager<User> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Redirect("/login");
        });
    }
}
