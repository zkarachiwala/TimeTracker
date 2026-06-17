using TimeTracker.Contracts.Features.Projects;

namespace TimeTracker.Client.Mock;

public class MockProjectService(MockDataStore store) : IProjectService
{
    public Task<List<ProjectResponse>> GetAllProjects(CancellationToken ct = default) =>
        Task.FromResult(store.Projects.ToList());

    public Task<List<ProjectResponse>> GetAssignedProjects(CancellationToken ct = default) =>
        Task.FromResult(store.Projects.ToList());

    public Task<ProjectResponse?> GetProjectById(int id, CancellationToken ct = default) =>
        Task.FromResult(store.Projects.FirstOrDefault(p => p.Id == id));

    public Task CreateProject(ProjectCreateRequest request, CancellationToken ct = default)
    {
        var client = request.ClientId.HasValue
            ? store.Clients.FirstOrDefault(c => c.Id == request.ClientId.Value)
            : null;
        store.Projects.Add(new ProjectResponse(
            store.NextProjectId(),
            request.Name,
            request.ClientId,
            client?.Name,
            request.HourlyRate,
            request.Description,
            request.StartDate,
            request.EndDate));
        return Task.CompletedTask;
    }

    public Task UpdateProject(int id, ProjectUpdateRequest request, CancellationToken ct = default)
    {
        var i = store.Projects.FindIndex(p => p.Id == id);
        if (i < 0) return Task.CompletedTask;

        var old = store.Projects[i];
        var client = request.ClientId.HasValue
            ? store.Clients.FirstOrDefault(c => c.Id == request.ClientId.Value)
            : null;
        store.Projects[i] = new ProjectResponse(
            old.Id,
            request.Name,
            request.ClientId,
            client?.Name,
            request.HourlyRate,
            request.Description,
            request.StartDate,
            request.EndDate);
        return Task.CompletedTask;
    }

    public Task DeleteProject(int id, CancellationToken ct = default)
    {
        store.Projects.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }

    public Task<List<DeletedProjectResponse>> GetDeletedProjects(CancellationToken ct = default) =>
        Task.FromResult(new List<DeletedProjectResponse>());

    public Task RestoreProject(int id, CancellationToken ct = default) => Task.CompletedTask;

    public Task<List<ProjectUserResponse>> GetProjectUsers(int projectId, CancellationToken ct = default) =>
        Task.FromResult(new List<ProjectUserResponse>());

    public Task AssignUserToProject(int projectId, string userId, CancellationToken ct = default) => Task.CompletedTask;

    public Task UnassignUserFromProject(int projectId, string userId, CancellationToken ct = default) => Task.CompletedTask;
}
