using Mapster;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;
using TimeTracker.Web.Data;

namespace TimeTracker.Web.Features.Clients;

public class ClientService : IClientService
{
    private readonly TimeTrackerDataContext _context;

    public ClientService(TimeTrackerDataContext context)
    {
        _context = context;
    }

    public async Task<List<ClientResponse>> GetAllClients(bool includeArchived = false)
    {
        var query = _context.Clients
            .Where(c => !c.IsDeleted)
            .Where(c => includeArchived || !c.IsArchived)
            .OrderBy(c => c.IsArchived)
            .ThenBy(c => c.Name);
        return (await query.ToListAsync()).Adapt<List<ClientResponse>>();
    }

    public async Task<ClientResponse?> GetClientById(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        return client?.Adapt<ClientResponse>();
    }

    public async Task CreateClient(ClientCreateRequest request)
    {
        var client = new Client
        {
            Name = request.Name,
            DefaultHourlyRate = request.DefaultHourlyRate,
            ContactName = request.ContactName,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone
        };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateClient(int id, ClientUpdateRequest request)
    {
        var client = await _context.Clients.FindAsync(id)
            ?? throw new EntityNotFoundException($"Client {id} not found.");

        client.Name = request.Name;
        client.DefaultHourlyRate = request.DefaultHourlyRate;
        client.ContactName = request.ContactName;
        client.ContactEmail = request.ContactEmail;
        client.ContactPhone = request.ContactPhone;
        client.DateUpdated = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task ArchiveClient(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new EntityNotFoundException($"Client {id} not found.");

        client.IsArchived = true;
        client.DateUpdated = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task UnarchiveClient(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new EntityNotFoundException($"Client {id} not found.");

        client.IsArchived = false;
        client.DateUpdated = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteClient(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted)
            ?? throw new EntityNotFoundException($"Client {id} not found.");

        var hasProjects = await _context.Projects.AnyAsync(p => p.ClientId == id && !p.IsDeleted);
        if (hasProjects)
            throw new InvalidOperationException("Cannot delete a client that has active projects.");

        client.IsDeleted = true;
        client.DateDeleted = DateTime.Now;
        await _context.SaveChangesAsync();
    }
}
