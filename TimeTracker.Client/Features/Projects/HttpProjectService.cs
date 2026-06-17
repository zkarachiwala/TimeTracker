using System.Net.Http.Json;
using TimeTracker.Contracts.Features.Projects;

namespace TimeTracker.Client.Features.Projects;

public class HttpProjectService(HttpClient http) : IProjectService
{
    public Task<List<ProjectResponse>> GetAllProjects(CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<ProjectResponse>>("api/projects/", ct)!;

    public Task<ProjectResponse?> GetProjectById(int id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ProjectResponse>($"api/projects/{id}", ct);

    public async Task CreateProject(ProjectCreateRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/projects/", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateProject(int id, ProjectUpdateRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/projects/{id}", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteProject(int id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"api/projects/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public Task<List<DeletedProjectResponse>> GetDeletedProjects(CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<DeletedProjectResponse>>("api/projects/deleted", ct)!;

    public async Task RestoreProject(int id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/projects/{id}/restore", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public Task<List<ProjectUserResponse>> GetProjectUsers(int projectId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<ProjectUserResponse>>($"api/projects/{projectId}/users", ct)!;

    public async Task AssignUserToProject(int projectId, string userId, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/projects/{projectId}/users", new AssignUserRequest(userId), ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UnassignUserFromProject(int projectId, string userId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"api/projects/{projectId}/users/{userId}", ct);
        response.EnsureSuccessStatusCode();
    }
}
