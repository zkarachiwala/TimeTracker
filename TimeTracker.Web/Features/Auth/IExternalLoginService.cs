using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Features.Auth;

public enum ExternalLoginStatus
{
    Success,
    EmailNotAllowed,
    CreateUserFailed,
    LinkProviderFailed
}

public record ExternalLoginResult(ExternalLoginStatus Status, User? User = null);

public interface IExternalLoginService
{
    Task<ExternalLoginResult> FindOrCreateUserAsync(string email, string loginProvider, string providerKey);
}
