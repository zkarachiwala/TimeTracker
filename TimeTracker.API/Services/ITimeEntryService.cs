using TimeTracker.Shared.Models;

namespace TimeTracker.API.Services;

public interface ITimeEntryService
{
    Task<TimeEntryResponse?> GetTimeEntryById(int id);
    Task<List<TimeEntryResponse>> GetAllTimeEntries();
    Task<List<TimeEntryResponse>> CreateTimeEntry(TimeEntryCreateRequest request);
    Task<List<TimeEntryResponse>?> UpdateTimeEntry(int id, TimeEntryUpdateRequest timeEntry);
    Task<List<TimeEntryResponse>?> DeleteTimeEntry(int id);
    Task<List<TimeEntryResponse>> GetAllTimeEntriesByProjectId(int projectId);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByProjectId(int projectId, int skip, int limit);
    Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit);
}

