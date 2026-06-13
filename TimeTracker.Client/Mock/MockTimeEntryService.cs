using TimeTracker.Contracts.Features.Projects;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Client.Mock;

public class MockTimeEntryService(MockDataStore store) : ITimeEntryService
{
    public Task<TimeEntryResponse?> GetActiveTimeEntry(CancellationToken ct = default) =>
        Task.FromResult(store.TimeEntries.FirstOrDefault(e => !e.End.HasValue));

    public Task<List<TimeEntryResponse>> GetTodaysTimeEntries(CancellationToken ct = default) =>
        Task.FromResult(store.TimeEntries
            .Where(e => e.Start.Date == DateTime.Today)
            .OrderByDescending(e => e.Start)
            .ToList());

    public Task<List<TimeEntryResponse>> GetAllTimeEntriesByYear(int year, CancellationToken ct = default) =>
        Task.FromResult(store.TimeEntries
            .Where(e => e.Start.Year == year)
            .OrderByDescending(e => e.Start)
            .ToList());

    public Task<List<TimeEntryResponse>> GetAllTimeEntriesByProject(int projectId, CancellationToken ct = default) =>
        Task.FromResult(store.TimeEntries
            .Where(e => e.Project.Id == projectId)
            .OrderByDescending(e => e.Start)
            .ToList());

    public Task<TimeEntryResponse?> GetTimeEntryById(int id, CancellationToken ct = default) =>
        Task.FromResult(store.TimeEntries.FirstOrDefault(e => e.Id == id));

    public Task CreateTimeEntry(TimeEntryCreateRequest request, CancellationToken ct = default)
    {
        var project = store.Projects.First(p => p.Id == request.ProjectId);
        store.TimeEntries.Add(new TimeEntryResponse(
            store.NextEntryId(),
            new ProjectSummary(project.Id, project.Name),
            request.Start,
            request.End,
            request.Note,
            null,
            null));
        return Task.CompletedTask;
    }

    public Task UpdateTimeEntry(int id, TimeEntryUpdateRequest request, CancellationToken ct = default)
    {
        var i = store.TimeEntries.FindIndex(e => e.Id == id);
        if (i < 0) return Task.CompletedTask;

        var old = store.TimeEntries[i];
        var project = request.ProjectId.HasValue
            ? store.Projects.First(p => p.Id == request.ProjectId.Value)
            : store.Projects.First(p => p.Id == old.Project.Id);

        store.TimeEntries[i] = new TimeEntryResponse(
            old.Id,
            new ProjectSummary(project.Id, project.Name),
            request.Start,
            request.End,
            request.Note,
            request.InvoiceReference,
            request.InvoicedAt);
        return Task.CompletedTask;
    }

    public Task DeleteTimeEntry(int id, CancellationToken ct = default)
    {
        store.TimeEntries.RemoveAll(e => e.Id == id);
        return Task.CompletedTask;
    }
}
