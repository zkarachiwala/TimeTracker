namespace TimeTracker.Contracts.Features.Clients;

public interface IClientService
{
    Task<List<ClientResponse>> GetAllClients(bool includeArchived = false);
    Task<ClientResponse?> GetClientById(int id);
    Task CreateClient(ClientCreateRequest request);
    Task UpdateClient(int id, ClientUpdateRequest request);
    Task ArchiveClient(int id);
    Task UnarchiveClient(int id);
    Task DeleteClient(int id);
}
