namespace TimeTracker.Contracts.Features.TimeEntries;

public interface ITimeEntryService
{
    Task<TimeEntryResponse?> GetTimeEntryById(int id, CancellationToken ct = default);
    Task CreateTimeEntry(TimeEntryCreateRequest request, CancellationToken ct = default);
    Task UpdateTimeEntry(int id, TimeEntryUpdateRequest request, CancellationToken ct = default);
    Task DeleteTimeEntry(int id, CancellationToken ct = default);
    Task<List<TimeEntryResponse>> GetAllTimeEntriesByYear(int year, CancellationToken ct = default);
    Task<TimeEntryResponse?> GetActiveTimeEntry(CancellationToken ct = default);
    Task<List<TimeEntryResponse>> GetTodaysTimeEntries(CancellationToken ct = default);
    Task<List<TimeEntryResponse>> GetAllTimeEntriesByProject(int projectId, CancellationToken ct = default);
}
