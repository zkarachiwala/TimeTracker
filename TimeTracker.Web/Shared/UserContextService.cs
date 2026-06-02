using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Shared;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<User> _userManager;
    private readonly AuthenticationStateProvider _authStateProvider;

    public UserContextService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<User> userManager,
        AuthenticationStateProvider authStateProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _authStateProvider = authStateProvider;
    }

    // Sync version — works for REST API / SSR where HttpContext is available
    public string? GetUserId() =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    // Async version — works for both SSR and Blazor InteractiveServer circuits
    public async Task<string?> GetUserIdAsync()
    {
        var fromHttp = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (fromHttp is not null) return fromHttp;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public async Task<User?> GetUserAsync()
    {
        var httpContextUser = _httpContextAccessor.HttpContext?.User;
        if (httpContextUser is not null)
            return await _userManager.GetUserAsync(httpContextUser);

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return await _userManager.GetUserAsync(authState.User);
    }
}
