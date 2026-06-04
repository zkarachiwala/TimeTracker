using System.Net.Http.Json;
using TimeTracker.Contracts.Features.Clients;

namespace TimeTracker.Wasm.Features.Clients;

public class HttpClientService(HttpClient http) : IClientService
{
    public Task<List<ClientResponse>> GetAllClients(bool includeArchived = false) =>
        http.GetFromJsonAsync<List<ClientResponse>>($"api/clients/?includeArchived={includeArchived}")!;

    public Task<ClientResponse?> GetClientById(int id) =>
        http.GetFromJsonAsync<ClientResponse>($"api/clients/{id}");

    public async Task CreateClient(ClientCreateRequest request)
    {
        var response = await http.PostAsJsonAsync("api/clients/", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateClient(int id, ClientUpdateRequest request)
    {
        var response = await http.PutAsJsonAsync($"api/clients/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task ArchiveClient(int id)
    {
        var response = await http.PostAsync($"api/clients/{id}/archive", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task UnarchiveClient(int id)
    {
        var response = await http.PostAsync($"api/clients/{id}/unarchive", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteClient(int id)
    {
        var response = await http.DeleteAsync($"api/clients/{id}");
        response.EnsureSuccessStatusCode();
    }
}
