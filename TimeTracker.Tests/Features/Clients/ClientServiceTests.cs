using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Features.Clients;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;
using Xunit;

namespace TimeTracker.Tests.Features.Clients;

[Collection("Services")]
public class ClientServiceTests
{
    private static TimeTrackerDataContext CreateContext() =>
        new(new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Client MakeClient(string name = "Acme Corp", decimal? rate = 150m, bool isArchived = false) =>
        new() { Name = name, DefaultHourlyRate = rate, IsArchived = isArchived };

    // --- GetAllClients ---

    [Fact]
    public async Task GetAllClients_ReturnsActiveClients()
    {
        using var context = CreateContext();
        context.Clients.AddRange(MakeClient("Client A"), MakeClient("Client B"));
        await context.SaveChangesAsync();

        var result = await new ClientService(context).GetAllClients();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllClients_ExcludesSoftDeleted()
    {
        using var context = CreateContext();
        var deleted = MakeClient("Deleted");
        deleted.IsDeleted = true;
        context.Clients.AddRange(MakeClient("Active"), deleted);
        await context.SaveChangesAsync();

        var result = await new ClientService(context).GetAllClients();

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public async Task GetAllClients_ExcludesArchivedByDefault()
    {
        using var context = CreateContext();
        context.Clients.AddRange(MakeClient("Active"), MakeClient("Old Client", isArchived: true));
        await context.SaveChangesAsync();

        var result = await new ClientService(context).GetAllClients();

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public async Task GetAllClients_IncludesArchivedWhenRequested()
    {
        using var context = CreateContext();
        context.Clients.AddRange(MakeClient("Active"), MakeClient("Archived", isArchived: true));
        await context.SaveChangesAsync();

        var result = await new ClientService(context).GetAllClients(includeArchived: true);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllClients_ReturnsOrderedByName()
    {
        using var context = CreateContext();
        context.Clients.AddRange(MakeClient("Zeta"), MakeClient("Alpha"), MakeClient("Mango"));
        await context.SaveChangesAsync();

        var result = await new ClientService(context).GetAllClients();

        Assert.Equal(new[] { "Alpha", "Mango", "Zeta" }, result.Select(c => c.Name));
    }

    // --- GetClientById ---

    [Fact]
    public async Task GetClientById_ReturnsClient()
    {
        using var context = CreateContext();
        var client = MakeClient("Acme Corp", 200m);
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var result = await new ClientService(context).GetClientById(client.Id);

        Assert.NotNull(result);
        Assert.Equal("Acme Corp", result.Name);
        Assert.Equal(200m, result.DefaultHourlyRate);
    }

    [Fact]
    public async Task GetClientById_ReturnsNull_WhenNotFound()
    {
        using var context = CreateContext();

        var result = await new ClientService(context).GetClientById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetClientById_ReturnsNull_WhenSoftDeleted()
    {
        using var context = CreateContext();
        var client = MakeClient();
        client.IsDeleted = true;
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var result = await new ClientService(context).GetClientById(client.Id);

        Assert.Null(result);
    }

    // --- CreateClient ---

    [Fact]
    public async Task CreateClient_PersistsClient()
    {
        using var context = CreateContext();

        await new ClientService(context).CreateClient(new ClientCreateRequest
        {
            Name = "New Client",
            DefaultHourlyRate = 175m
        });

        var client = context.Clients.Single();
        Assert.Equal("New Client", client.Name);
        Assert.Equal(175m, client.DefaultHourlyRate);
    }

    [Fact]
    public async Task CreateClient_AllowsNullRate()
    {
        using var context = CreateContext();

        await new ClientService(context).CreateClient(new ClientCreateRequest
        {
            Name = "No Rate Client",
            DefaultHourlyRate = null
        });

        Assert.Null(context.Clients.Single().DefaultHourlyRate);
    }

    // --- UpdateClient ---

    [Fact]
    public async Task UpdateClient_UpdatesNameAndRate()
    {
        using var context = CreateContext();
        var client = MakeClient("Old Name", 100m);
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        await new ClientService(context).UpdateClient(client.Id, new ClientUpdateRequest
        {
            Name = "New Name",
            DefaultHourlyRate = 250m
        });

        var updated = context.Clients.Single();
        Assert.Equal("New Name", updated.Name);
        Assert.Equal(250m, updated.DefaultHourlyRate);
        Assert.NotNull(updated.DateUpdated);
    }

    [Fact]
    public async Task UpdateClient_ThrowsEntityNotFoundException_WhenNotFound()
    {
        using var context = CreateContext();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            new ClientService(context).UpdateClient(999, new ClientUpdateRequest { Name = "X" }));
    }

    // --- ArchiveClient / UnarchiveClient ---

    [Fact]
    public async Task ArchiveClient_SetsIsArchived()
    {
        using var context = CreateContext();
        var client = MakeClient();
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        await new ClientService(context).ArchiveClient(client.Id);

        Assert.True(context.Clients.Single().IsArchived);
    }

    [Fact]
    public async Task UnarchiveClient_ClearsIsArchived()
    {
        using var context = CreateContext();
        var client = MakeClient(isArchived: true);
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        await new ClientService(context).UnarchiveClient(client.Id);

        Assert.False(context.Clients.Single().IsArchived);
    }

    [Fact]
    public async Task ArchiveClient_ThrowsEntityNotFoundException_WhenNotFound()
    {
        using var context = CreateContext();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            new ClientService(context).ArchiveClient(999));
    }

    // --- DeleteClient ---

    [Fact]
    public async Task DeleteClient_SoftDeletesClient()
    {
        using var context = CreateContext();
        var client = MakeClient();
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        await new ClientService(context).DeleteClient(client.Id);

        var deleted = context.Clients.Single();
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DateDeleted);
    }

    [Fact]
    public async Task DeleteClient_DoesNotHardDeleteRecord()
    {
        using var context = CreateContext();
        var client = MakeClient();
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        await new ClientService(context).DeleteClient(client.Id);

        Assert.Equal(1, context.Clients.Count());
    }

    [Fact]
    public async Task DeleteClient_ThrowsEntityNotFoundException_WhenNotFound()
    {
        using var context = CreateContext();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            new ClientService(context).DeleteClient(999));
    }

    [Fact]
    public async Task DeleteClient_ThrowsInvalidOperationException_WhenClientHasActiveProjects()
    {
        using var context = CreateContext();
        var client = MakeClient();
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        context.Projects.Add(new Project
        {
            Name = "Active Project",
            ClientId = client.Id,
            ProjectUsers = [new ProjectUser { UserId = "user-1" }]
        });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ClientService(context).DeleteClient(client.Id));
    }

    [Fact]
    public async Task DeleteClient_SucceedsWhenProjectIsSoftDeleted()
    {
        using var context = CreateContext();
        var client = MakeClient();
        context.Clients.Add(client);
        await context.SaveChangesAsync();

        context.Projects.Add(new Project
        {
            Name = "Deleted Project",
            ClientId = client.Id,
            IsDeleted = true,
            ProjectUsers = [new ProjectUser { UserId = "user-1" }]
        });
        await context.SaveChangesAsync();

        await new ClientService(context).DeleteClient(client.Id);

        Assert.True(context.Clients.Single().IsDeleted);
    }
}
