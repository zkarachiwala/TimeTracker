using TimeTracker.Shared.Models;
using TimeTracker.Shared.Models.TimeEntry;

namespace TimeTracker.Client.Services;

public interface ITimeEntryService
{
    event Action? OnChange;

    event Action? ProjectChanged;

    int SelectedProjectId { get; set; }

    public void SetSelectedProject(int projectId);

    public List<TimeEntryResponse> TimeEntries { get; set; }
    
    Task GetTimeEntriesByProject(int projectId);

    Task<TimeEntryResponseWrapper> GetTimeEntriesByProject(int projectId, int skip, int limit);

    Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit);

    Task<TimeEntryResponse> GetTimeEntryById(int id);

    Task CreateTimeEntry(TimeEntryRequest request);

    Task UpdateTimeEntry(int id, TimeEntryRequest request);

    Task DeleteTimeEntry(int id);
}