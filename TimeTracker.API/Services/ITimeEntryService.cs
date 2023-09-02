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
    Task<TimeEntryResponseWrapper> GetTimeEntriesByYear(int year, int skip = ServiceConstants.DEFAULT_SKIP, int limit = ServiceConstants.DEFAULT_LIMIT);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByMonth(int month, int year, int skip = ServiceConstants.DEFAULT_SKIP, int limit = ServiceConstants.DEFAULT_LIMIT);
    Task<TimeEntryResponseWrapper>  GetTimeEntriesByDay(int day, int month, int year, int skip = ServiceConstants.DEFAULT_SKIP, int limit = ServiceConstants.DEFAULT_LIMIT);    
}

