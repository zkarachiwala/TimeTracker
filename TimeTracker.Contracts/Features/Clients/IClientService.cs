namespace TimeTracker.Contracts.Features.Clients;

public interface IClientService
{
    Task<List<ClientResponse>> GetAllClients(bool includeArchived = false, CancellationToken ct = default);
    Task<ClientResponse?> GetClientById(int id, CancellationToken ct = default);
    Task CreateClient(ClientCreateRequest request, CancellationToken ct = default);
    Task UpdateClient(int id, ClientUpdateRequest request, CancellationToken ct = default);
    Task ArchiveClient(int id, CancellationToken ct = default);
    Task UnarchiveClient(int id, CancellationToken ct = default);
    Task DeleteClient(int id, CancellationToken ct = default);
    Task<List<DeletedClientResponse>> GetDeletedClients(CancellationToken ct = default);
    Task RestoreClient(int id, CancellationToken ct = default);
}
