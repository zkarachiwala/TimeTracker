using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TimeTracker.Client.Mock;

public class MockAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "demo@example.com"),
            new Claim(ClaimTypes.Email, "demo@example.com"),
            new Claim(ClaimTypes.Role, "Admin"),
        };
        var identity = new ClaimsIdentity(claims, "Demo");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
}
