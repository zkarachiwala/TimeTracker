using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Features.Clients;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;
using TimeTracker.Tests.Infrastructure;
using Xunit;

namespace TimeTracker.Tests.Features.Clients;

[Collection("Services")]
public class ClientServiceTests
{
    private static DbContextOptions<TimeTrackerDataContext> CreateOptions() =>
        new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static ClientService CreateService(DbContextOptions<TimeTrackerDataContext> options) =>
        new(new TestDbContextFactory(options));

    private static TimeTracker.Shared.Entities.Client MakeClient(string name = "Acme Corp", decimal? rate = 150m, bool isArchived = false) =>
        new() { Name = name, DefaultHourlyRate = rate, IsArchived = isArchived };

    [Fact]
    public async Task GetAllClients_ReturnsActiveClients()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        seed.Clients.AddRange(MakeClient("Client A"), MakeClient("Client B"));
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetAllClients();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllClients_ExcludesSoftDeleted()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var deleted = MakeClient("Deleted");
        deleted.IsDeleted = true;
        seed.Clients.AddRange(MakeClient("Active"), deleted);
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetAllClients();
        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public async Task GetAllClients_ExcludesArchivedByDefault()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        seed.Clients.AddRange(MakeClient("Active"), MakeClient("Old Client", isArchived: true));
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetAllClients();
        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public async Task GetAllClients_IncludesArchivedWhenRequested()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        seed.Clients.AddRange(MakeClient("Active"), MakeClient("Archived", isArchived: true));
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetAllClients(includeArchived: true);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllClients_ReturnsOrderedByName()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        seed.Clients.AddRange(MakeClient("Zeta"), MakeClient("Alpha"), MakeClient("Mango"));
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetAllClients();
        Assert.Equal(new[] { "Alpha", "Mango", "Zeta" }, result.Select(c => c.Name));
    }

    [Fact]
    public async Task GetClientById_ReturnsClient()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var client = MakeClient("Acme Corp", 200m);
        seed.Clients.Add(client);
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetClientById(client.Id);
        Assert.NotNull(result);
        Assert.Equal("Acme Corp", result.Name);
        Assert.Equal(200m, result.DefaultHourlyRate);
    }

    [Fact]
    public async Task GetClientById_ReturnsNull_WhenNotFound()
    {
        var options = CreateOptions();
        var result = await CreateService(options).GetClientById(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetClientById_ReturnsNull_WhenSoftDeleted()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var client = MakeClient();
        client.IsDeleted = true;
        seed.Clients.Add(client);
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetClientById(client.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateClient_PersistsClient()
    {
        var options = CreateOptions();
        await CreateService(options).CreateClient(new ClientCreateRequest { Name = "New Client", DefaultHourlyRate = 175m });

        using var context = new TimeTrackerDataContext(options);
        var client = context.Clients.Single();
        Assert.Equal("New Client", client.Name);
        Assert.Equal(175m, client.DefaultHourlyRate);
    }

    [Fact]
    public async Task CreateClient_AllowsNullRate()
    {
        var options = CreateOptions();
        await CreateService(options).CreateClient(new ClientCreateRequest { Name = "No Rate Client", DefaultHourlyRate = null });

        using var context = new TimeTrackerDataContext(options);
        Assert.Null(context.Clients.Single().DefaultHourlyRate);
    }

    [Fact]
    public async Task UpdateClient_UpdatesNameAndRate()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var client = MakeClient("Old Name", 100m);
        seed.Clients.Add(client);
        await seed.SaveChangesAsync();

        await CreateService(options).UpdateClient(client.Id, new ClientUpdateRequest { Name = "New Name", DefaultHourlyRate = 250m });

        using var context = new TimeTrackerDataContext(options);
        var updated = context.Clients.Single();
        Assert.Equal("New Name", updated.Name);
        Assert.Equal(250m, updated.DefaultHourlyRate);
        Assert.NotNull(updated.DateUpdated);
    }

    [Fact]
    public async Task UpdateClient_ThrowsEntityNotFoundException_WhenNotFound()
    {
        var options = CreateOptions();
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            CreateService(options).UpdateClient(999, new ClientUpdateRequest { Name = "X" }));
    }

    [Fact]
    public async Task ArchiveClient_SetsIsArchived()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var client = MakeClient();
        seed.Clients.Add(client);
        await seed.SaveChangesAsync();

        await CreateService(options).ArchiveClient(client.Id);

        using var context = new TimeTrackerDataContext(options);
        Assert.True(context.Clients.Single().IsArchived);
    }

    [Fact]
    public async Task UnarchiveClient_ClearsIsArchived()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var client = MakeClient(isArchived: true);
        seed.Clients.Add(client);
        await seed.SaveChangesAsync();

        await CreateService(options).UnarchiveClient(client.Id);

        using var context = new TimeTrackerDataContext(options);
        Assert.False(context.Clients.Single().IsArchived);
    }

    [Fact]
    public async Task ArchiveClient_ThrowsEntityNotFoundException_WhenNotFound()
    {
        var options = CreateOptions();
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            CreateService(options).ArchiveClient(999));
    }

    [Fact]
    public async Task DeleteClient_SoftDeletesClient()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var client = MakeClient();
        seed.Clients.Add(client);
        await seed.SaveChangesAsync();

        await CreateService(options).DeleteClient(client.Id);

        using var context = new TimeTrackerDataContext(options);
        var deleted = context.Clients.Single();
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DateDeleted);
    }

    [Fact]
    public async Task DeleteClient_DoesNotHardDeleteRecord()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var client = MakeClient();
        seed.Clients.Add(client);
        await seed.SaveChangesAsync();

        await CreateService(options).DeleteClient(client.Id);

        using var context = new TimeTrackerDataContext(options);
        Assert.Equal(1, context.Clients.Count());
    }

    [Fact]
    public async Task DeleteClient_ThrowsEntityNotFoundException_WhenNotFound()
    {
        var options = CreateOptions();
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            CreateService(options).DeleteClient(999));
    }

    [Fact]
    public async Task DeleteClient_ThrowsInvalidOperationException_WhenClientHasActiveProjects()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var client = MakeClient();
        seed.Clients.Add(client);
        await seed.SaveChangesAsync();
        seed.Projects.Add(new Project { Name = "Active Project", ClientId = client.Id, ProjectUsers = [new ProjectUser { UserId = "user-1" }] });
        await seed.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(options).DeleteClient(client.Id));
    }

    [Fact]
    public async Task DeleteClient_SucceedsWhenProjectIsSoftDeleted()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var client = MakeClient();
        seed.Clients.Add(client);
        await seed.SaveChangesAsync();
        seed.Projects.Add(new Project { Name = "Deleted Project", ClientId = client.Id, IsDeleted = true, ProjectUsers = [new ProjectUser { UserId = "user-1" }] });
        await seed.SaveChangesAsync();

        await CreateService(options).DeleteClient(client.Id);

        using var context = new TimeTrackerDataContext(options);
        Assert.True(context.Clients.Single().IsDeleted);
    }

    // --- GetDeletedClients / RestoreClient ---

    [Fact]
    public async Task GetDeletedClients_ReturnsOnlySoftDeletedClients()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var deleted = MakeClient("Deleted");
        deleted.IsDeleted = true;
        seed.Clients.AddRange(MakeClient("Active"), deleted);
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetDeletedClients();

        Assert.Single(result);
        Assert.Equal("Deleted", result[0].Name);
    }

    [Fact]
    public async Task GetDeletedClients_ExcludesActiveClients()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        seed.Clients.AddRange(MakeClient("Active A"), MakeClient("Active B"));
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetDeletedClients();

        Assert.Empty(result);
    }

    [Fact]
    public async Task RestoreClient_ClearsIsDeletedAndDateDeleted()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var client = MakeClient("Was Deleted");
        client.IsDeleted = true;
        client.DateDeleted = DateTime.Now;
        seed.Clients.Add(client);
        await seed.SaveChangesAsync();

        await CreateService(options).RestoreClient(client.Id);

        using var context = new TimeTrackerDataContext(options);
        var restored = context.Clients.Single();
        Assert.False(restored.IsDeleted);
        Assert.Null(restored.DateDeleted);
    }

    [Fact]
    public async Task RestoreClient_MakesClientVisibleInNormalQuery()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var client = MakeClient("Was Deleted");
        client.IsDeleted = true;
        seed.Clients.Add(client);
        await seed.SaveChangesAsync();

        await CreateService(options).RestoreClient(client.Id);

        var result = await CreateService(options).GetAllClients();
        Assert.Single(result);
        Assert.Equal("Was Deleted", result[0].Name);
    }

    [Fact]
    public async Task RestoreClient_ThrowsEntityNotFoundException_WhenNotFound()
    {
        var options = CreateOptions();
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            CreateService(options).RestoreClient(999));
    }
}
