using System.Net.Http.Json;
using Mapster;
using TimeTracker.Shared.Models.Project;

namespace TimeTracker.Client.Services;

public class ProjectService : IProjectService
{
    private readonly HttpClient _http;

    public event Action? OnChange;

    public ProjectService(HttpClient http)
    {
        _http = http;
    }
    public List<ProjectResponse> Projects { get ; set; } = new List<ProjectResponse>();

    public async Task LoadAllProjects()
    {
        var result = await _http.GetFromJsonAsync<List<ProjectResponse>>("api/project");

        if (result is not null)
        {
            Projects = result;
            OnChange?.Invoke();
        }
    }

    public async Task CreateProject(ProjectRequest request)
    {
        await _http.PostAsJsonAsync("api/project", request.Adapt<ProjectCreateRequest>());
    }

    public async Task UpdateProject(int id, ProjectRequest request)
    {
        await _http.PutAsJsonAsync($"api/project/{id}", request.Adapt<ProjectUpdateRequest>());
    }

    public async Task DeleteProject(int id)
    {
        await _http.DeleteAsync($"api/project/{id}");
    }

    public async Task<ProjectResponse> GetProjectById(int id)
    {
        return await _http.GetFromJsonAsync<ProjectResponse>($"api/project/{id}");
    }
}