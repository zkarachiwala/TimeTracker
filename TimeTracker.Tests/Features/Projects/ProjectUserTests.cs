using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Features.Projects;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;
using TimeTracker.Tests.Infrastructure;
using Xunit;

namespace TimeTracker.Tests.Features.Projects;

[Collection("Services")]
public class ProjectUserTests
{
    private const string UserId = "user-1";

    private static DbContextOptions<TimeTrackerDataContext> CreateOptions() =>
        new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static ProjectService CreateService(DbContextOptions<TimeTrackerDataContext> options, IdentityFixture fixture) =>
        new(new TestDbContextFactory(options), new FakeUserContextService(UserId), fixture.UserManager);

    private static Project SeedProject(TimeTrackerDataContext ctx, string ownerId)
    {
        var project = new Project
        {
            Name = "Test Project",
            ProjectUsers = [new ProjectUser { UserId = ownerId }]
        };
        ctx.Projects.Add(project);
        ctx.SaveChanges();
        return project;
    }

    [Fact]
    public async Task GetProjectUsers_returns_assigned_users_with_emails()
    {
        using var fixture = new IdentityFixture();
        var options = CreateOptions();

        var user = new User { Id = UserId, UserName = "owner@example.com", Email = "owner@example.com" };
        await fixture.UserManager.CreateAsync(user);

        using var ctx = new TimeTrackerDataContext(options);
        var project = SeedProject(ctx, UserId);

        var result = await CreateService(options, fixture).GetProjectUsers(project.Id);

        Assert.Single(result);
        Assert.Equal(UserId, result[0].UserId);
        Assert.Equal("owner@example.com", result[0].Email);
    }

    [Fact]
    public async Task GetProjectUsers_returns_empty_for_project_with_no_users()
    {
        using var fixture = new IdentityFixture();
        var options = CreateOptions();

        using var ctx = new TimeTrackerDataContext(options);
        var project = new Project { Name = "Empty Project" };
        ctx.Projects.Add(project);
        await ctx.SaveChangesAsync();

        var result = await CreateService(options, fixture).GetProjectUsers(project.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AssignUser_adds_user_to_project()
    {
        using var fixture = new IdentityFixture();
        var options = CreateOptions();
        const string newUserId = "user-2";

        using var ctx = new TimeTrackerDataContext(options);
        var project = SeedProject(ctx, UserId);

        await CreateService(options, fixture).AssignUserToProject(project.Id, newUserId);

        using var verify = new TimeTrackerDataContext(options);
        Assert.True(await verify.ProjectUsers.AnyAsync(pu => pu.ProjectId == project.Id && pu.UserId == newUserId));
    }

    [Fact]
    public async Task AssignUser_throws_if_already_assigned()
    {
        using var fixture = new IdentityFixture();
        var options = CreateOptions();

        using var ctx = new TimeTrackerDataContext(options);
        var project = SeedProject(ctx, UserId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(options, fixture).AssignUserToProject(project.Id, UserId));
    }

    [Fact]
    public async Task UnassignUser_removes_user_from_project()
    {
        using var fixture = new IdentityFixture();
        var options = CreateOptions();
        const string secondUserId = "user-2";

        using var ctx = new TimeTrackerDataContext(options);
        var project = SeedProject(ctx, UserId);
        ctx.ProjectUsers.Add(new ProjectUser { ProjectId = project.Id, UserId = secondUserId });
        await ctx.SaveChangesAsync();

        await CreateService(options, fixture).UnassignUserFromProject(project.Id, secondUserId);

        using var verify = new TimeTrackerDataContext(options);
        Assert.False(await verify.ProjectUsers.AnyAsync(pu => pu.ProjectId == project.Id && pu.UserId == secondUserId));
    }

    [Fact]
    public async Task UnassignUser_throws_if_not_assigned()
    {
        using var fixture = new IdentityFixture();
        var options = CreateOptions();

        using var ctx = new TimeTrackerDataContext(options);
        var project = SeedProject(ctx, UserId);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            CreateService(options, fixture).UnassignUserFromProject(project.Id, "nonexistent-user"));
    }
}
