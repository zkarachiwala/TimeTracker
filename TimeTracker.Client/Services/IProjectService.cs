using TimeTracker.Shared.Models.Project;

namespace TimeTracker.Client.Services;

public interface IProjectService
{
    event Action? OnChange;
    public List<ProjectResponse> Projects { get; set; }

    public Task LoadAllProjects();
    
}