using System.Net.Http.Json;
using Mapster;
using TimeTracker.Shared.Models;
using TimeTracker.Shared.Models.TimeEntry;

namespace TimeTracker.Client.Services;

public class TimeEntryService : ITimeEntryService
{
    private readonly HttpClient _http;

    public List<TimeEntryResponse> TimeEntries { get; set; } = new List<TimeEntryResponse>();

    public int SelectedProjectId { get; set; } = 0;
    public int SelectedDay { get; set; } = DateTime.Now.Day;
    public int SelectedMonth { get; set; } = DateTime.Now.Month;
    public int SelectedYear { get; set; } = DateTime.Now.Year;
    public TimeSpan TotalDuration { get; set; }

    public event Action? OnChange;
    public event Action? FilterChanged;

    public TimeEntryService(HttpClient http)
    {
        _http = http;
    }

    public async Task GetTimeEntriesByProject(int projectId)
    {
        List<TimeEntryResponse>? result;
        if (projectId <= 0)
        {
            result = await _http.GetFromJsonAsync<List<TimeEntryResponse>>("api/timeentry");
        }
        else
        {
            result = await _http.GetFromJsonAsync<List<TimeEntryResponse>>($"api/timeentry/project/{projectId}");
        }

        SetTimeEntries(result);
    }

    private void SetTimeEntries(List<TimeEntryResponse>? result)
    {
        if (result is not null)
        {
            TimeEntries = result;
            CalculateTotalDuration();
            OnChange?.Invoke();
        }
    }

    public void RefreshData()
    {
        OnChange?.Invoke();
    }

    public async Task<TimeEntryResponse> GetTimeEntryById(int id)
    {
        return await _http.GetFromJsonAsync<TimeEntryResponse>($"api/timeentry/{id}");
    }

    public async Task CreateTimeEntry(TimeEntryRequest request)
    {
        await _http.PostAsJsonAsync("api/timeentry", request.Adapt<TimeEntryCreateRequest>());
    }

    public async Task UpdateTimeEntry(int id, TimeEntryRequest request)
    {
        await _http.PutAsJsonAsync($"api/timeentry/{id}", request.Adapt<TimeEntryUpdateRequest>());
    }

    public async Task DeleteTimeEntry(int id)
    {
        await _http.DeleteAsync($"api/timeentry/{id}");
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByProject(int projectId, int skip, int limit)
    {
        TimeEntryResponseWrapper result;
        if (projectId <= 0)
        {
            result = await GetTimeEntries(skip, limit);
        }
        else
        {
            result = await _http.GetFromJsonAsync<TimeEntryResponseWrapper>($"/api/timeentry/project/{projectId}/{skip}/{limit}");
        }

        if(result!.TimeEntries is not null)
        {
            TimeEntries = result!.TimeEntries;
            OnChange?.Invoke();
        }
        return result;       
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit)
    {
        return await _http.GetFromJsonAsync<TimeEntryResponseWrapper>($"/api/timeentry/{skip}/{limit}");
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByYear(int year, int skip, int limit)
    {
        var result = await _http.GetFromJsonAsync<TimeEntryResponseWrapper>($"api/timeentry/year/{year}/{skip}/{limit}");

        SetTimeEntries(result!.TimeEntries);
        return result;
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByMonth(int month, int year, int skip, int limit)
    {
        var result = await _http.GetFromJsonAsync<TimeEntryResponseWrapper>($"api/timeentry/month/{month}/year/{year}/{skip}/{limit}");
        SetTimeEntries(result!.TimeEntries);
        return result;
    }

    public async Task<TimeEntryResponseWrapper> GetTimeEntriesByDay(int day, int month, int year, int skip, int limit)
    {
        var result = await _http.GetFromJsonAsync<TimeEntryResponseWrapper>(
                $"api/timeentry/day/{day}/month/{month}/year/{year}/{skip}/{limit}");
        SetTimeEntries(result!.TimeEntries);
        return result;
    }

    private void SetSelectedFilter(int day = 0, int month = 0, int year = 0, int projectId = 0)
    {
        SelectedProjectId = projectId;
        SelectedDay = day;
        SelectedMonth = month;
        SelectedYear = year;
        FilterChanged?.Invoke();
    }

    public void SetSelectedProject(int projectId) => SetSelectedFilter(projectId: projectId);
    public void SetSelectedDay(int day, int month, int year) => SetSelectedFilter(day, month, year);
    public void SetSelectedMonth(int month, int year) => SetSelectedFilter(0, month, year);
    public void SetSelectedYear(int year) => SetSelectedFilter(0, 0, year);

    private TimeSpan CalculateDuration(TimeEntryResponse timeEntry)
    {
        if(timeEntry.End is null || timeEntry.End < timeEntry.Start)
            return new TimeSpan();
        
        TimeSpan duration = timeEntry.End.Value - timeEntry.Start;
        return duration;
    }

    private void CalculateTotalDuration()
    {
        TotalDuration = new TimeSpan();
        foreach (var timeEntry in TimeEntries)
        {
            TotalDuration += CalculateDuration(timeEntry);
        }
    }
}