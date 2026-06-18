using System.Net.Http.Json;
using TimeTracker.Contracts.Features.Admin;

namespace TimeTracker.Client.Features.Admin;

public class HttpUserManagementService(HttpClient http) : IUserManagementService
{
    public Task<List<AdminUserResponse>> GetUsersAsync(CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<AdminUserResponse>>("api/admin/users", ct)!;

    public async Task AddUserAsync(string email, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/admin/users", new AddUserRequest(email), ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetAdminRoleAsync(string userId, bool isAdmin, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/admin/users/{userId}/role", new SetAdminRoleRequest(isAdmin), ct);
        response.EnsureSuccessStatusCode();
    }
}
