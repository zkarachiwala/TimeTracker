using TimeTracker.Client.Shared;
using TimeTracker.Contracts.Features.Projects;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.ComponentTests.Features.Timer;

internal class FakeProjectService(List<ProjectResponse> projects) : IProjectService
{
    public Task<List<ProjectResponse>> GetAllProjects(CancellationToken ct = default) => Task.FromResult(projects);
    public Task<List<ProjectResponse>> GetAssignedProjects(CancellationToken ct = default) => Task.FromResult(projects);
    public Task<ProjectResponse?> GetProjectById(int id, CancellationToken ct = default) =>
        Task.FromResult(projects.FirstOrDefault(p => p.Id == id));
    public Task CreateProject(ProjectCreateRequest request, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateProject(int id, ProjectUpdateRequest request, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteProject(int id, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<DeletedProjectResponse>> GetDeletedProjects(CancellationToken ct = default) =>
        Task.FromResult(new List<DeletedProjectResponse>());
    public Task RestoreProject(int id, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<ProjectUserResponse>> GetProjectUsers(int projectId, CancellationToken ct = default) =>
        Task.FromResult(new List<ProjectUserResponse>());
    public Task AssignUserToProject(int projectId, string userId, CancellationToken ct = default) => Task.CompletedTask;
    public Task UnassignUserFromProject(int projectId, string userId, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Test double for ITimeEntryService whose UpdateTimeEntry can be made to fail
/// (simulating a backend that is asleep/unreachable) and records every call it receives.</summary>
internal class FakeTimeEntryService : ITimeEntryService
{
    public TimeEntryResponse? ActiveEntry { get; set; }
    public List<TimeEntryResponse> TodaysEntries { get; set; } = [];
    public bool FailUpdates { get; set; }
    public List<(int Id, TimeEntryUpdateRequest Request)> UpdateCalls { get; } = [];

    public Task<TimeEntryResponse?> GetActiveTimeEntry(CancellationToken ct = default) => Task.FromResult(ActiveEntry);
    public Task<List<TimeEntryResponse>> GetTodaysTimeEntries(CancellationToken ct = default) => Task.FromResult(TodaysEntries);
    public Task<List<TimeEntryResponse>> GetAllTimeEntriesByYear(int year, CancellationToken ct = default) =>
        Task.FromResult(new List<TimeEntryResponse>());
    public Task<List<TimeEntryResponse>> GetAllTimeEntriesByProject(int projectId, CancellationToken ct = default) =>
        Task.FromResult(new List<TimeEntryResponse>());
    public Task<TimeEntryResponse?> GetTimeEntryById(int id, CancellationToken ct = default) => Task.FromResult(ActiveEntry);
    public Task CreateTimeEntry(TimeEntryCreateRequest request, CancellationToken ct = default) => Task.CompletedTask;

    public Task UpdateTimeEntry(int id, TimeEntryUpdateRequest request, CancellationToken ct = default)
    {
        UpdateCalls.Add((id, request));
        if (FailUpdates)
            throw new HttpRequestException("Simulated backend unreachable");

        if (ActiveEntry is not null && ActiveEntry.Id == id)
            ActiveEntry = ActiveEntry with { End = request.End };
        return Task.CompletedTask;
    }

    public Task DeleteTimeEntry(int id, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<DeletedTimeEntryResponse>> GetDeletedTimeEntries(CancellationToken ct = default) =>
        Task.FromResult(new List<DeletedTimeEntryResponse>());
    public Task RestoreTimeEntry(int id, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>In-memory stand-in for browser localStorage.</summary>
internal class FakeLocalStore : ILocalStore
{
    private readonly Dictionary<string, string> _store = [];

    public ValueTask<string?> GetItemAsync(string key) =>
        ValueTask.FromResult(_store.GetValueOrDefault(key));

    public ValueTask SetItemAsync(string key, string value)
    {
        _store[key] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveItemAsync(string key)
    {
        _store.Remove(key);
        return ValueTask.CompletedTask;
    }

    public bool Contains(string key) => _store.ContainsKey(key);
}
