namespace TimeTracker.Contracts.Features.Projects;

public interface IProjectService
{
    Task<List<ProjectResponse>> GetAllProjects(CancellationToken ct = default);
    Task<ProjectResponse?> GetProjectById(int id, CancellationToken ct = default);
    Task CreateProject(ProjectCreateRequest request, CancellationToken ct = default);
    Task UpdateProject(int id, ProjectUpdateRequest request, CancellationToken ct = default);
    Task DeleteProject(int id, CancellationToken ct = default);
}
