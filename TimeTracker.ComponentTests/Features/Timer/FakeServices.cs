using TimeTracker.Client.Shared;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.ComponentTests.Features.Timer;

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
    public bool FailCreates { get; set; }
    public List<TimeEntryCreateRequest> CreateCalls { get; } = [];
    private readonly Dictionary<Guid, TimeEntryResponse> _createdByClientRequestId = [];
    private int _nextId = 1000;

    public Task<TimeEntryResponse> CreateTimeEntry(TimeEntryCreateRequest request, CancellationToken ct = default)
    {
        CreateCalls.Add(request);

        // Mirrors the real server's idempotency: a repeated ClientRequestId returns the entry
        // already created instead of a new one — checked before FailCreates, matching the real
        // server (a retry that already landed should succeed even if the connection is currently
        // flaky, since idempotency is checked before any new work is attempted).
        if (request.ClientRequestId is { } tag && _createdByClientRequestId.TryGetValue(tag, out var existing))
            return Task.FromResult(existing);

        if (FailCreates)
            throw new HttpRequestException("Simulated backend unreachable");

        var entry = new TimeEntryResponse(_nextId++, new ProjectSummary(request.ProjectId, "Project"),
            request.Start, request.End, request.Note, null, null);
        if (request.ClientRequestId is { } newTag) _createdByClientRequestId[newTag] = entry;
        return Task.FromResult(entry);
    }

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
