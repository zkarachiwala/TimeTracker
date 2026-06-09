using TimeTracker.Contracts.Features.Clients;

namespace TimeTracker.Client.Mock;

public class MockClientService(MockDataStore store) : IClientService
{
    public Task<List<ClientResponse>> GetAllClients(bool includeArchived = false) =>
        Task.FromResult(store.Clients
            .Where(c => includeArchived || !c.IsArchived)
            .ToList());

    public Task<ClientResponse?> GetClientById(int id) =>
        Task.FromResult(store.Clients.FirstOrDefault(c => c.Id == id));

    public Task CreateClient(ClientCreateRequest request)
    {
        store.Clients.Add(new ClientResponse(
            store.NextClientId(),
            request.Name,
            false,
            request.DefaultHourlyRate,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone));
        return Task.CompletedTask;
    }

    public Task UpdateClient(int id, ClientUpdateRequest request)
    {
        var i = store.Clients.FindIndex(c => c.Id == id);
        if (i < 0) return Task.CompletedTask;

        var old = store.Clients[i];
        store.Clients[i] = new ClientResponse(
            old.Id,
            request.Name,
            old.IsArchived,
            request.DefaultHourlyRate,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone);
        return Task.CompletedTask;
    }

    public Task ArchiveClient(int id)
    {
        var i = store.Clients.FindIndex(c => c.Id == id);
        if (i >= 0) store.Clients[i] = store.Clients[i] with { IsArchived = true };
        return Task.CompletedTask;
    }

    public Task UnarchiveClient(int id)
    {
        var i = store.Clients.FindIndex(c => c.Id == id);
        if (i >= 0) store.Clients[i] = store.Clients[i] with { IsArchived = false };
        return Task.CompletedTask;
    }

    public Task DeleteClient(int id)
    {
        store.Clients.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}
