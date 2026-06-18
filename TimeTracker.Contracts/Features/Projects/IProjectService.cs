namespace TimeTracker.Contracts.Features.Projects;

public interface IProjectService
{
    Task<List<ProjectResponse>> GetAllProjects(CancellationToken ct = default);
    Task<List<ProjectResponse>> GetAssignedProjects(CancellationToken ct = default);
    Task<ProjectResponse?> GetProjectById(int id, CancellationToken ct = default);
    Task CreateProject(ProjectCreateRequest request, CancellationToken ct = default);
    Task UpdateProject(int id, ProjectUpdateRequest request, CancellationToken ct = default);
    Task DeleteProject(int id, CancellationToken ct = default);
    Task<List<DeletedProjectResponse>> GetDeletedProjects(CancellationToken ct = default);
    Task RestoreProject(int id, CancellationToken ct = default);
    Task<List<ProjectUserResponse>> GetProjectUsers(int projectId, CancellationToken ct = default);
    Task AssignUserToProject(int projectId, string userId, CancellationToken ct = default);
    Task UnassignUserFromProject(int projectId, string userId, CancellationToken ct = default);
}
