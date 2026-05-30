namespace TimeTracker.API.Features.Auth;

public interface IAuthService
{
    Task<(bool Succeeded, string? Error)> LoginAsync(string userName, string password);
    Task LogoutAsync();
    Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(string userName, string email, string password);
}
