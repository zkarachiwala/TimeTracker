using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Features.Projects;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;
using TimeTracker.Tests.Infrastructure;
using Xunit;

namespace TimeTracker.Tests.Features.Projects;

[Collection("Services")]
public class ProjectServiceTests
{
    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    private static DbContextOptions<TimeTrackerDataContext> CreateOptions() =>
        new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static ProjectService CreateService(DbContextOptions<TimeTrackerDataContext> options, string userId = UserId, UserManager<User>? userManager = null) =>
        new(new TestDbContextFactory(options), new FakeUserContextService(userId), userManager!);

    private static Project MakeProject(string userId, string name = "Project", bool isDeleted = false, decimal? hourlyRate = 100m, int? clientId = null) =>
        new()
        {
            Name = name,
            IsDeleted = isDeleted,
            HourlyRate = hourlyRate,
            ClientId = clientId,
            Description = "Details",
            ProjectUsers = [new ProjectUser { UserId = userId }]
        };

    [Fact]
    public async Task GetAllProjects_ReturnsAllNonDeletedProjects()
    {
        var options = CreateOptions();
        using var context = new TimeTrackerDataContext(options);
        context.Projects.AddRange(MakeProject(UserId, "My Project"), MakeProject(OtherUserId, "Other Project"));
        await context.SaveChangesAsync();

        var result = await CreateService(options).GetAllProjects();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAssignedProjects_ReturnsOnlyProjectsAssignedToCurrentUser()
    {
        var options = CreateOptions();
        using var context = new TimeTrackerDataContext(options);
        context.Projects.AddRange(MakeProject(UserId, "My Project"), MakeProject(OtherUserId, "Other Project"));
        await context.SaveChangesAsync();

        var result = await CreateService(options).GetAssignedProjects();

        Assert.Single(result);
        Assert.Equal("My Project", result[0].Name);
    }

    [Fact]
    public async Task GetAssignedProjects_ExcludesSoftDeletedProjects()
    {
        var options = CreateOptions();
        using var context = new TimeTrackerDataContext(options);
        context.Projects.AddRange(MakeProject(UserId, "Active"), MakeProject(UserId, "Deleted", isDeleted: true));
        await context.SaveChangesAsync();

        var result = await CreateService(options).GetAssignedProjects();

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public async Task GetAllProjects_ExcludesSoftDeletedProjects()
    {
        var options = CreateOptions();
        using var context = new TimeTrackerDataContext(options);
        context.Projects.AddRange(MakeProject(UserId, "Active"), MakeProject(UserId, "Deleted", isDeleted: true));
        await context.SaveChangesAsync();

        var result = await CreateService(options).GetAllProjects();

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public async Task GetProjectById_ReturnsProjectWithDetails()
    {
        var options = CreateOptions();
        using var context = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId, "My Project");
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var result = await CreateService(options).GetProjectById(project.Id);

        Assert.NotNull(result);
        Assert.Equal("My Project", result.Name);
        Assert.Equal("Details", result.Description);
    }

    [Fact]
    public async Task GetProjectById_ReturnsNull_WhenSoftDeleted()
    {
        var options = CreateOptions();
        using var context = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId, isDeleted: true);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var result = await CreateService(options).GetProjectById(project.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProjectById_ReturnsProject_WhenUserNotAssigned()
    {
        var options = CreateOptions();
        using var context = new TimeTrackerDataContext(options);
        var project = MakeProject(OtherUserId, "Other Project");
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // All projects are visible to all users (single-tenant, D023)
        var result = await CreateService(options).GetProjectById(project.Id);

        Assert.NotNull(result);
        Assert.Equal("Other Project", result.Name);
    }

    [Fact]
    public async Task CreateProject_CreatesWithDetails()
    {
        var options = CreateOptions();
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        await CreateService(options).CreateProject(new ProjectCreateRequest
        {
            Name = "New Project",
            Description = "A description",
            StartDate = startDate,
            EndDate = endDate,
            HourlyRate = 150m
        });

        using var context = new TimeTrackerDataContext(options);
        var project = context.Projects.Single();
        Assert.Equal("New Project", project.Name);
        Assert.Equal("A description", project.Description);
        Assert.Equal(startDate, project.StartDate);
        Assert.Equal(endDate, project.EndDate);
    }

    [Fact]
    public async Task CreateProject_PersistsHourlyRate()
    {
        var options = CreateOptions();
        await CreateService(options).CreateProject(new ProjectCreateRequest { Name = "Billed Project", HourlyRate = 175m });

        using var context = new TimeTrackerDataContext(options);
        Assert.Equal(175m, context.Projects.Single().HourlyRate);
    }

    [Fact]
    public async Task CreateProject_AddsCurrentUserToProjectUsers()
    {
        var options = CreateOptions();
        await CreateService(options).CreateProject(new ProjectCreateRequest { Name = "New Project" });

        using var context = new TimeTrackerDataContext(options);
        var project = context.Projects.Include(p => p.ProjectUsers).Single();
        Assert.Single(project.ProjectUsers);
        Assert.Equal(UserId, project.ProjectUsers[0].UserId);
    }

    [Fact]
    public async Task UpdateProject_UpdatesNameAndDetails()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId, "Old Name");
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();

        await CreateService(options).UpdateProject(project.Id, new ProjectUpdateRequest
        {
            Name = "New Name",
            Description = "Updated description",
            StartDate = new DateTime(2025, 1, 1),
            HourlyRate = 200m
        });

        using var context = new TimeTrackerDataContext(options);
        var updated = context.Projects.Single();
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(new DateTime(2025, 1, 1), updated.StartDate);
    }

    [Fact]
    public async Task UpdateProject_PersistsEndDate_ForArchive()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();

        var endDate = new DateTime(2026, 6, 1);
        await CreateService(options).UpdateProject(project.Id, new ProjectUpdateRequest
        {
            Name = project.Name,
            EndDate = endDate
        });

        using var context = new TimeTrackerDataContext(options);
        Assert.Equal(endDate, context.Projects.Single().EndDate);
    }

    [Fact]
    public async Task UpdateProject_ClearsEndDate_ForUnarchive()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        project.EndDate = new DateTime(2026, 1, 1);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();

        await CreateService(options).UpdateProject(project.Id, new ProjectUpdateRequest
        {
            Name = project.Name,
            EndDate = null
        });

        using var context = new TimeTrackerDataContext(options);
        Assert.Null(context.Projects.Single().EndDate);
    }

    [Fact]
    public async Task UpdateProject_UpdatesHourlyRate()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId, hourlyRate: 100m);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();

        await CreateService(options).UpdateProject(project.Id, new ProjectUpdateRequest { Name = project.Name, HourlyRate = 250m });

        using var context = new TimeTrackerDataContext(options);
        Assert.Equal(250m, context.Projects.Single().HourlyRate);
    }

    [Fact]
    public async Task DeleteProject_SoftDeletesProject()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();

        await CreateService(options).DeleteProject(project.Id);

        using var context = new TimeTrackerDataContext(options);
        var deleted = context.Projects.Single();
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DateDeleted);
    }

    [Fact]
    public async Task DeleteProject_DoesNotHardDeleteRecord()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();

        await CreateService(options).DeleteProject(project.Id);

        using var context = new TimeTrackerDataContext(options);
        Assert.Equal(1, context.Projects.Count());
    }

    [Fact]
    public async Task DeleteProject_ThrowsEntityNotFoundException_WhenAlreadyDeleted()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId, isDeleted: true);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            CreateService(options).DeleteProject(project.Id));
    }

    [Fact]
    public async Task UpdateProject_ThrowsEntityNotFoundException_WhenNotFound()
    {
        var options = CreateOptions();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            CreateService(options).UpdateProject(999, new ProjectUpdateRequest { Name = "Updated" }));
    }

    [Fact]
    public async Task CreateProject_DoesNotSetRefCode()
    {
        var options = CreateOptions();

        await CreateService(options).CreateProject(new ProjectCreateRequest { Name = "New Project" });

        using var context = new TimeTrackerDataContext(options);
        Assert.Equal(string.Empty, context.Projects.Single().RefCode);
    }

    [Fact]
    public async Task UpdateProject_DoesNotModifyRefCode()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        project.RefCode = "PROJ-001";
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();

        await CreateService(options).UpdateProject(project.Id, new ProjectUpdateRequest { Name = "Updated Name", HourlyRate = 200m });

        using var context = new TimeTrackerDataContext(options);
        Assert.Equal("PROJ-001", context.Projects.Single().RefCode);
    }

    // --- GetDeletedProjects / RestoreProject ---

    [Fact]
    public async Task GetDeletedProjects_ReturnsOnlySoftDeletedProjects()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        seed.Projects.AddRange(MakeProject(UserId, "Active"), MakeProject(UserId, "Deleted", isDeleted: true));
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetDeletedProjects();

        Assert.Single(result);
        Assert.Equal("Deleted", result[0].Name);
    }

    [Fact]
    public async Task GetDeletedProjects_ReturnsDeletedProjectsFromAllUsers()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        seed.Projects.AddRange(
            MakeProject(UserId, "User1 Deleted", isDeleted: true),
            MakeProject(OtherUserId, "User2 Deleted", isDeleted: true));
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetDeletedProjects();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task RestoreProject_ClearsIsDeletedAndDateDeleted()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId, "Was Deleted", isDeleted: true);
        project.DateDeleted = DateTime.Now;
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();

        await CreateService(options).RestoreProject(project.Id);

        using var context = new TimeTrackerDataContext(options);
        var restored = context.Projects.Single();
        Assert.False(restored.IsDeleted);
        Assert.Null(restored.DateDeleted);
    }

    [Fact]
    public async Task RestoreProject_MakesProjectVisibleInNormalQuery()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId, "Was Deleted", isDeleted: true);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();

        await CreateService(options).RestoreProject(project.Id);

        var result = await CreateService(options).GetAllProjects();
        Assert.Single(result);
        Assert.Equal("Was Deleted", result[0].Name);
    }

    [Fact]
    public async Task RestoreProject_ThrowsEntityNotFoundException_WhenNotFound()
    {
        var options = CreateOptions();
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            CreateService(options).RestoreProject(999));
    }
}
