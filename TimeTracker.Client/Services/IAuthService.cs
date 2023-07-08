using TimeTracker.Shared.Models.Account;

namespace TimeTracker.Client.Services;

public interface IAuthService
{
    Task Register(AccountRegistrationRequest request);
}