using TimeTracker.Contracts.Features.Admin;

namespace TimeTracker.Web.Features.Admin;

public interface IUserManagementService
{
    Task<IReadOnlyList<AdminUserResponse>> GetUsersAsync(CancellationToken ct = default);
    Task<AddUserResult> AddUserAsync(string email, CancellationToken ct = default);
    Task<SetAdminRoleResult> SetAdminRoleAsync(string userId, bool isAdmin, CancellationToken ct = default);
}

public enum SetAdminRoleResult { Success, NotFound, LastAdmin }
public enum AddUserResult { Success, AlreadyExists, Failed }
