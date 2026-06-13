using System.Net;
using System.Net.Http.Json;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Client.Features.TimeEntries;

public class HttpTimeEntryService(HttpClient http) : ITimeEntryService
{
    public async Task<TimeEntryResponse?> GetActiveTimeEntry(CancellationToken ct = default)
    {
        var response = await http.GetAsync("api/timeentries/active", ct);
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TimeEntryResponse>(ct);
    }

    public Task<List<TimeEntryResponse>> GetTodaysTimeEntries(CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<TimeEntryResponse>>("api/timeentries/today", ct)!;

    public Task<List<TimeEntryResponse>> GetAllTimeEntriesByYear(int year, CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<TimeEntryResponse>>($"api/timeentries/year/{year}/all", ct)!;

    public async Task CreateTimeEntry(TimeEntryCreateRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/timeentries/", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateTimeEntry(int id, TimeEntryUpdateRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/timeentries/{id}", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteTimeEntry(int id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"api/timeentries/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public Task<TimeEntryResponse?> GetTimeEntryById(int id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<TimeEntryResponse>($"api/timeentries/{id}", ct);

    public Task<List<TimeEntryResponse>> GetAllTimeEntriesByProject(int projectId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<TimeEntryResponse>>($"api/timeentries/project/{projectId}/all", ct)!;
}
