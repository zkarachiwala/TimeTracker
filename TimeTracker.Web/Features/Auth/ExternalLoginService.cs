using Microsoft.AspNetCore.Identity;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Features.Auth;

public class ExternalLoginService(UserManager<User> userManager) : IExternalLoginService
{
    public async Task<ExternalLoginResult> FindOrCreateUserAsync(string email, string loginProvider, string providerKey)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return new ExternalLoginResult(ExternalLoginStatus.EmailNotAllowed);

        var existingLogins = await userManager.GetLoginsAsync(user);
        if (!existingLogins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey))
        {
            var loginInfo = new UserLoginInfo(loginProvider, providerKey, loginProvider);
            var addLoginResult = await userManager.AddLoginAsync(user, loginInfo);
            if (!addLoginResult.Succeeded)
                return new ExternalLoginResult(ExternalLoginStatus.LinkProviderFailed);
        }

        return new ExternalLoginResult(ExternalLoginStatus.Success, user);
    }
}
