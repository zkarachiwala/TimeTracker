namespace TimeTracker.Contracts.Features.TimeEntries;

public interface ITimeEntryService
{
    Task<TimeEntryResponse?> GetTimeEntryById(int id);
    Task CreateTimeEntry(TimeEntryCreateRequest request);
    Task UpdateTimeEntry(int id, TimeEntryUpdateRequest request);
    Task DeleteTimeEntry(int id);
    Task<List<TimeEntryResponse>> GetAllTimeEntriesByYear(int year);
    Task<TimeEntryResponse?> GetActiveTimeEntry();
    Task<List<TimeEntryResponse>> GetTodaysTimeEntries();
    Task<List<TimeEntryResponse>> GetAllTimeEntriesByProject(int projectId);
}
