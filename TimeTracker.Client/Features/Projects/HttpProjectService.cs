using System.Net.Http.Json;
using TimeTracker.Contracts.Features.Projects;

namespace TimeTracker.Wasm.Features.Projects;

public class HttpProjectService(HttpClient http) : IProjectService
{
    public Task<List<ProjectResponse>> GetAllProjects() =>
        http.GetFromJsonAsync<List<ProjectResponse>>("api/projects/")!;

    public Task<ProjectResponse?> GetProjectById(int id) =>
        http.GetFromJsonAsync<ProjectResponse>($"api/projects/{id}");

    public async Task CreateProject(ProjectCreateRequest request)
    {
        var response = await http.PostAsJsonAsync("api/projects/", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateProject(int id, ProjectUpdateRequest request)
    {
        var response = await http.PutAsJsonAsync($"api/projects/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteProject(int id)
    {
        var response = await http.DeleteAsync($"api/projects/{id}");
        response.EnsureSuccessStatusCode();
    }
}
