using System.Net.Http.Json;
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
}