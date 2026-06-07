namespace TimeTracker.Contracts.Features.TimeEntries;

public interface ITimeEntryQueryService
{
    Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByProjectId(int projectId, int skip, int limit);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByYear(int year, int skip, int limit);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByMonth(int month, int year, int skip, int limit);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByDay(int day, int month, int year, int skip, int limit);
}
