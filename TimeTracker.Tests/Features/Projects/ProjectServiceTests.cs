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

    private static TimeTrackerDataContext CreateContext() =>
        new(new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Project MakeProject(string userId, string name = "Project", bool isDeleted = false, bool includeDetails = true) =>
        new()
        {
            Name = name,
            IsDeleted = isDeleted,
            ProjectDetails = includeDetails ? new ProjectDetails { Description = "Details", Project = null! } : null,
            ProjectUsers = [new ProjectUser { UserId = userId }]
        };

    [Fact]
    public async Task GetAllProjects_ReturnsOnlyUserProjects()
    {
        using var context = CreateContext();
        context.Projects.AddRange(
            MakeProject(UserId, "My Project"),
            MakeProject(OtherUserId, "Other Project"));
        await context.SaveChangesAsync();

        var result = await new ProjectService(context, new FakeUserContextService(UserId))
            .GetAllProjects();

        Assert.Single(result);
        Assert.Equal("My Project", result[0].Name);
    }

    [Fact]
    public async Task GetAllProjects_ExcludesSoftDeletedProjects()
    {
        using var context = CreateContext();
        context.Projects.AddRange(
            MakeProject(UserId, "Active"),
            MakeProject(UserId, "Deleted", isDeleted: true));
        await context.SaveChangesAsync();

        var result = await new ProjectService(context, new FakeUserContextService(UserId))
            .GetAllProjects();

        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public async Task GetProjectById_ReturnsProjectWithDetails()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId, "My Project");
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var result = await new ProjectService(context, new FakeUserContextService(UserId))
            .GetProjectById(project.Id);

        Assert.NotNull(result);
        Assert.Equal("My Project", result.Name);
        Assert.Equal("Details", result.Description);
    }

    [Fact]
    public async Task GetProjectById_ReturnsNull_WhenSoftDeleted()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId, isDeleted: true);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var result = await new ProjectService(context, new FakeUserContextService(UserId))
            .GetProjectById(project.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProjectById_ReturnsNull_WhenUserNotMember()
    {
        using var context = CreateContext();
        var project = MakeProject(OtherUserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var result = await new ProjectService(context, new FakeUserContextService(UserId))
            .GetProjectById(project.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateProject_CreatesWithProjectDetails()
    {
        using var context = CreateContext();
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        await new ProjectService(context, new FakeUserContextService(UserId))
            .CreateProject(new ProjectCreateRequest
            {
                Name = "New Project",
                Description = "A description",
                StartDate = startDate,
                EndDate = endDate
            });

        var project = context.Projects.Single();
        Assert.Equal("New Project", project.Name);
        Assert.NotNull(project.ProjectDetails);
        Assert.Equal("A description", project.ProjectDetails.Description);
        Assert.Equal(startDate, project.ProjectDetails.StartDate);
        Assert.Equal(endDate, project.ProjectDetails.EndDate);
    }

    [Fact]
    public async Task CreateProject_AddsCurrentUserToProjectUsers()
    {
        using var context = CreateContext();

        await new ProjectService(context, new FakeUserContextService(UserId))
            .CreateProject(new ProjectCreateRequest { Name = "New Project" });

        var project = context.Projects.Include(p => p.ProjectUsers).Single();
        Assert.Single(project.ProjectUsers);
        Assert.Equal(UserId, project.ProjectUsers[0].UserId);
    }

    [Fact]
    public async Task UpdateProject_UpdatesNameAndDetails()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId, "Old Name");
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        await new ProjectService(context, new FakeUserContextService(UserId))
            .UpdateProject(project.Id, new ProjectUpdateRequest
            {
                Name = "New Name",
                Description = "Updated description",
                StartDate = new DateTime(2025, 1, 1)
            });

        var updated = context.Projects.Single();
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("Updated description", updated.ProjectDetails!.Description);
        Assert.Equal(new DateTime(2025, 1, 1), updated.ProjectDetails.StartDate);
    }

    [Fact]
    public async Task UpdateProject_CreatesProjectDetails_WhenMissing()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId, includeDetails: false);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        await new ProjectService(context, new FakeUserContextService(UserId))
            .UpdateProject(project.Id, new ProjectUpdateRequest
            {
                Name = "Updated",
                Description = "New details"
            });

        var updated = context.Projects.Single();
        Assert.NotNull(updated.ProjectDetails);
        Assert.Equal("New details", updated.ProjectDetails.Description);
    }

    [Fact]
    public async Task DeleteProject_SoftDeletesProject()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        await new ProjectService(context, new FakeUserContextService(UserId))
            .DeleteProject(project.Id);

        var deleted = context.Projects.Single();
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DateDeleted);
    }

    [Fact]
    public async Task DeleteProject_DoesNotHardDeleteRecord()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        await new ProjectService(context, new FakeUserContextService(UserId))
            .DeleteProject(project.Id);

        Assert.Equal(1, context.Projects.Count());
    }

    [Fact]
    public async Task DeleteProject_ThrowsEntityNotFoundException_WhenAlreadyDeleted()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId, isDeleted: true);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            new ProjectService(context, new FakeUserContextService(UserId))
                .DeleteProject(project.Id));
    }

    [Fact]
    public async Task UpdateProject_ThrowsEntityNotFoundException_WhenNotFound()
    {
        using var context = CreateContext();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            new ProjectService(context, new FakeUserContextService(UserId))
                .UpdateProject(999, new ProjectUpdateRequest { Name = "Updated" }));
    }
}
