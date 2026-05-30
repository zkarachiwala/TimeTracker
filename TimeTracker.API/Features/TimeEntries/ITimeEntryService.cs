namespace TimeTracker.API.Features.TimeEntries;

public interface ITimeEntryService
{
    Task<TimeEntryResponse?> GetTimeEntryById(int id);
    Task CreateTimeEntry(TimeEntryCreateRequest request);
    Task UpdateTimeEntry(int id, TimeEntryUpdateRequest request);
    Task DeleteTimeEntry(int id);
    Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByProjectId(int projectId, int skip, int limit);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByYear(int year, int skip, int limit);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByMonth(int month, int year, int skip, int limit);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByDay(int day, int month, int year, int skip, int limit);
    Task<List<TimeEntryResponse>> GetAllTimeEntriesByYear(int year);
}
