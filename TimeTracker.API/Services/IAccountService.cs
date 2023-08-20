using TimeTracker.Shared.Models.Account;

namespace TimeTracker.API.Services;

public interface IAccountService
{
    Task<AccountRegistrationResponse> RegisterAsync(AccountRegistrationRequest request);
    Task AssignRole(string userName, string roleName);
    Task<List<String>> GetRolesAsync(User user);
}