using Microsoft.AspNetCore.Identity;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Features.Auth;

public class ExternalLoginService(UserManager<User> userManager, IConfiguration configuration) : IExternalLoginService
{
    public async Task<ExternalLoginResult> FindOrCreateUserAsync(string email, string loginProvider, string providerKey)
    {
        var allowedEmails = configuration.GetSection("Authentication:AllowedEmails").Get<string[]>() ?? [];
        if (!allowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase))
            return new ExternalLoginResult(ExternalLoginStatus.EmailNotAllowed);

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return new ExternalLoginResult(ExternalLoginStatus.CreateUserFailed);
        }

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
