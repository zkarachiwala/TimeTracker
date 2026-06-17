using TimeTracker.Contracts.Features.Admin;

namespace TimeTracker.Client.Features.Admin;

public interface IUserManagementService
{
    Task<List<AdminUserResponse>> GetUsersAsync(CancellationToken ct = default);
    Task SetAdminRoleAsync(string userId, bool isAdmin, CancellationToken ct = default);
}
