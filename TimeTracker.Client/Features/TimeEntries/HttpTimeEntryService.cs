using System.Net;
using System.Net.Http.Json;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Client.Features.TimeEntries;

public class HttpTimeEntryService(HttpClient http) : ITimeEntryService
{
    public async Task<TimeEntryResponse?> GetActiveTimeEntry()
    {
        var response = await http.GetAsync("api/timeentries/active");
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TimeEntryResponse>();
    }

    public Task<List<TimeEntryResponse>> GetTodaysTimeEntries() =>
        http.GetFromJsonAsync<List<TimeEntryResponse>>("api/timeentries/today")!;

    public Task<List<TimeEntryResponse>> GetAllTimeEntriesByYear(int year) =>
        http.GetFromJsonAsync<List<TimeEntryResponse>>($"api/timeentries/year/{year}/all")!;

    public async Task CreateTimeEntry(TimeEntryCreateRequest request)
    {
        var response = await http.PostAsJsonAsync("api/timeentries/", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateTimeEntry(int id, TimeEntryUpdateRequest request)
    {
        var response = await http.PutAsJsonAsync($"api/timeentries/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteTimeEntry(int id)
    {
        var response = await http.DeleteAsync($"api/timeentries/{id}");
        response.EnsureSuccessStatusCode();
    }

    public Task<TimeEntryResponse?> GetTimeEntryById(int id) =>
        http.GetFromJsonAsync<TimeEntryResponse>($"api/timeentries/{id}");

    public Task<List<TimeEntryResponse>> GetAllTimeEntriesByProject(int projectId) =>
        http.GetFromJsonAsync<List<TimeEntryResponse>>($"api/timeentries/project/{projectId}/all")!;
}
