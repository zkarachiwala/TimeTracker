using TimeTracker.Shared.Models;
using TimeTracker.Shared.Models.TimeEntry;

namespace TimeTracker.Client.Services;

public interface ITimeEntryService
{
    event Action? OnChange;

    event Action? FilterChanged;

    int SelectedProjectId { get; set; }
    int SelectedDay { get; set; }
    int SelectedMonth { get; set; }
    int SelectedYear { get; set; }
    TimeSpan TotalDuration { get; set; }

    public void SetSelectedProject(int projectId);
    public void SetSelectedDay(int day, int month, int year);
    public void SetSelectedMonth(int month, int year);
    public void SetSelectedYear(int year);

    public List<TimeEntryResponse> TimeEntries { get; set; }
    
    Task GetTimeEntriesByProject(int projectId);

    Task<TimeEntryResponseWrapper> GetTimeEntriesByProject(int projectId, int skip, int limit);

    Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit);

    Task<TimeEntryResponse> GetTimeEntryById(int id);

    Task CreateTimeEntry(TimeEntryRequest request);

    Task UpdateTimeEntry(int id, TimeEntryRequest request);

    Task DeleteTimeEntry(int id);

    void RefreshData();

    Task<TimeEntryResponseWrapper> GetTimeEntriesByYear(int year, int skip, int limit);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByMonth(int month, int year, int skip, int limit);
    Task<TimeEntryResponseWrapper> GetTimeEntriesByDay(int day, int month, int year, int skip, int limit);
}