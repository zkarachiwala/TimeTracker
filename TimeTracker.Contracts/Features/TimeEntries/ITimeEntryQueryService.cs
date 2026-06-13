namespace TimeTracker.Contracts.Features.TimeEntries;

public interface ITimeEntryQueryService
{
    Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit, CancellationToken ct = default);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByProjectId(int projectId, int skip, int limit, CancellationToken ct = default);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByYear(int year, int skip, int limit, CancellationToken ct = default);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByMonth(int month, int year, int skip, int limit, CancellationToken ct = default);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByDay(int day, int month, int year, int skip, int limit, CancellationToken ct = default);
}
