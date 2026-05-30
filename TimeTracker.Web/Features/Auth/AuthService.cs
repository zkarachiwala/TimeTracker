using Microsoft.AspNetCore.Identity;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Features.Auth;

public class AuthService : IAuthService
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public AuthService(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<(bool Succeeded, string? Error)> LoginAsync(string userName, string password)
    {
        var result = await _signInManager.PasswordSignInAsync(userName, password, isPersistent: false, lockoutOnFailure: false);
        if (!result.Succeeded)
            return (false, "Username or password is incorrect.");
        return (true, null);
    }

    public async Task LogoutAsync() => await _signInManager.SignOutAsync();

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(string userName, string email, string password)
    {
        var user = new User { UserName = userName, Email = email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, password);
        return result.Succeeded
            ? (true, Enumerable.Empty<string>())
            : (false, result.Errors.Select(e => e.Description));
    }
}
