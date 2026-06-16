using System.Net.Http.Json;
using TimeTracker.Contracts.Features.Clients;

namespace TimeTracker.Client.Features.Clients;

public class HttpClientService(HttpClient http) : IClientService
{
    public Task<List<ClientResponse>> GetAllClients(bool includeArchived = false, CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<ClientResponse>>($"api/clients/?includeArchived={includeArchived}", ct)!;

    public Task<ClientResponse?> GetClientById(int id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ClientResponse>($"api/clients/{id}", ct);

    public async Task CreateClient(ClientCreateRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/clients/", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateClient(int id, ClientUpdateRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/clients/{id}", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ArchiveClient(int id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/clients/{id}/archive", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task UnarchiveClient(int id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/clients/{id}/unarchive", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteClient(int id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"api/clients/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public Task<List<DeletedClientResponse>> GetDeletedClients(CancellationToken ct = default) =>
        http.GetFromJsonAsync<List<DeletedClientResponse>>("api/clients/deleted", ct)!;

    public async Task RestoreClient(int id, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/clients/{id}/restore", null, ct);
        response.EnsureSuccessStatusCode();
    }
}
