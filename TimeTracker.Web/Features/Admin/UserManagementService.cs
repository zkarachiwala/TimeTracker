using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Contracts.Features.Admin;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Features.Admin;

public class UserManagementService(UserManager<User> userManager) : IUserManagementService
{
    public async Task<IReadOnlyList<AdminUserResponse>> GetUsersAsync(CancellationToken ct = default)
    {
        var users = await userManager.Users.OrderBy(u => u.Email).ToListAsync(ct);
        var result = new List<AdminUserResponse>(users.Count);
        foreach (var user in users)
        {
            var isAdmin = await userManager.IsInRoleAsync(user, "Admin");
            result.Add(new AdminUserResponse(user.Id, user.Email!, isAdmin));
        }
        return result;
    }

    public async Task<AddUserResult> AddUserAsync(string email, CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return AddUserResult.AlreadyExists;

        var user = new User { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user);
        return result.Succeeded ? AddUserResult.Success : AddUserResult.Failed;
    }

    public async Task<SetAdminRoleResult> SetAdminRoleAsync(string userId, bool isAdmin, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return SetAdminRoleResult.NotFound;

        if (!isAdmin)
        {
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count == 1 && admins[0].Id == userId)
                return SetAdminRoleResult.LastAdmin;
        }

        var currentlyAdmin = await userManager.IsInRoleAsync(user, "Admin");
        if (isAdmin == currentlyAdmin) return SetAdminRoleResult.Success;

        var result = isAdmin
            ? await userManager.AddToRoleAsync(user, "Admin")
            : await userManager.RemoveFromRoleAsync(user, "Admin");

        return result.Succeeded ? SetAdminRoleResult.Success : SetAdminRoleResult.NotFound;
    }
}
