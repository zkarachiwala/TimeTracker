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

        UserInfoResponse? info;
        try
        {
            info = await http.GetFromJsonAsync<UserInfoResponse>("/api/auth/user");
        }
        catch
        {
            // Transport failure (backend asleep/unreachable) is not a definitive answer —
            // leave state uncached so the next call retries instead of permanently locking
            // this page load into Anonymous once the backend wakes back up.
            return Anonymous();
        }

        if (info is null || !info.IsAuthenticated)
            return _cached = Anonymous();

        var claims = new List<Claim> { new(ClaimTypes.Name, info.Email!) };
        claims.AddRange(info.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var identity = new ClaimsIdentity(claims, "Cookie");
        return _cached = new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <summary>Clears the cached auth state and re-notifies subscribers once connectivity returns.</summary>
    public void Refresh()
    {
        _cached = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
