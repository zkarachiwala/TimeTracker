using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TimeTracker.Contracts.Auth;

namespace TimeTracker.Client.Features.Auth;

public class CookieAuthenticationStateProvider(HttpClient http) : AuthenticationStateProvider
{
    private AuthenticationState? _cached;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_cached is not null) return _cached;
        try
        {
            var info = await http.GetFromJsonAsync<UserInfoResponse>("/api/auth/user");
            if (info is null || !info.IsAuthenticated)
                return _cached = Anonymous();

            var claims = new List<Claim> { new(ClaimTypes.Name, info.Email!) };
            claims.AddRange(info.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
            var identity = new ClaimsIdentity(claims, "Cookie");
            return _cached = new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return _cached = Anonymous();
        }
    }

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
