using TimeTracker.Contracts.Features.Admin;

namespace TimeTracker.Client.Mock;

public class MockUserManagementService : IUserManagementService
{
    private static readonly List<AdminUserResponse> _users =
    [
        new("1", "demo@example.com", true)
    ];

    public Task<List<AdminUserResponse>> GetUsersAsync(CancellationToken ct = default) =>
        Task.FromResult(_users);

    public Task AddUserAsync(string email, CancellationToken ct = default)
    {
        _users.Add(new AdminUserResponse((_users.Count + 1).ToString(), email, false));
        return Task.CompletedTask;
    }

    public Task SetAdminRoleAsync(string userId, bool isAdmin, CancellationToken ct = default)
    {
        var index = _users.FindIndex(u => u.Id == userId);
        if (index >= 0)
            _users[index] = _users[index] with { IsAdmin = isAdmin };
        return Task.CompletedTask;
    }
}
