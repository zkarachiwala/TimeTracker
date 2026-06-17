using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Contracts.Features.Admin;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.Admin;

public class UserManagementService(UserManager<User> userManager) : IUserManagementService
{
    public async Task<List<AdminUserResponse>> GetUsersAsync(CancellationToken ct = default)
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

    public async Task AddUserAsync(string email, CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            throw new InvalidOperationException($"A user with email '{email}' already exists.");

        var user = new User { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    public async Task SetAdminRoleAsync(string userId, bool isAdmin, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException($"User '{userId}' not found.");

        if (!isAdmin)
        {
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count == 1 && admins[0].Id == userId)
                throw new InvalidOperationException("Cannot remove the last admin.");
        }

        var currentlyAdmin = await userManager.IsInRoleAsync(user, "Admin");
        if (isAdmin == currentlyAdmin) return;

        var result = isAdmin
            ? await userManager.AddToRoleAsync(user, "Admin")
            : await userManager.RemoveFromRoleAsync(user, "Admin");

        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to update role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }
}
