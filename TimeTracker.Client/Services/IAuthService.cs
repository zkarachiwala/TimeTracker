using TimeTracker.Shared.Models.Account;
using TimeTracker.Shared.Models.Login;

namespace TimeTracker.Client.Services;

public interface IAuthService
{
    Task Register(AccountRegistrationRequest request);
    Task Login(LoginRequest request);
    Task Logout();
}