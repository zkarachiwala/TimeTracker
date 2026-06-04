using System.Net.Http.Json;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Wasm.Features.TimeEntries;

public class HttpTimeEntryService(HttpClient http) : ITimeEntryService
{
    public Task<TimeEntryResponse?> GetActiveTimeEntry() =>
        http.GetFromJsonAsync<TimeEntryResponse>("api/timeentries/active");

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

    public Task<TimeEntryResponseWrapper> GetTimeEntries(int skip, int limit) =>
        throw new NotSupportedException();

    public Task<TimeEntryResponseWrapper> GetTimeEntriesByProjectId(int projectId, int skip, int limit) =>
        throw new NotSupportedException();

    public Task<TimeEntryResponseWrapper> GetTimeEntriesByYear(int year, int skip, int limit) =>
        throw new NotSupportedException();

    public Task<TimeEntryResponseWrapper> GetTimeEntriesByMonth(int month, int year, int skip, int limit) =>
        throw new NotSupportedException();

    public Task<TimeEntryResponseWrapper> GetTimeEntriesByDay(int day, int month, int year, int skip, int limit) =>
        throw new NotSupportedException();

    public Task<List<TimeEntryResponse>> GetAllTimeEntriesByProject(int projectId) =>
        http.GetFromJsonAsync<List<TimeEntryResponse>>($"api/timeentries/project/{projectId}/all")!;
}
