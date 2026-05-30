using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Features.TimeEntries;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;
using TimeTracker.Tests.Infrastructure;
using Xunit;

namespace TimeTracker.Tests.Features.TimeEntries;

[Collection("Services")]
public class TimeEntryServiceTests
{
    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    private static TimeTrackerDataContext CreateContext() =>
        new(new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Project MakeProject(string userId, string name = "Project", bool isDeleted = false) =>
        new()
        {
            Name = name,
            IsDeleted = isDeleted,
            ProjectDetails = new ProjectDetails { Project = null! },
            ProjectUsers = [new ProjectUser { UserId = userId }]
        };

    [Fact]
    public async Task GetTimeEntries_ReturnsOnlyCurrentUserEntries()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = project.Id, UserId = OtherUserId, Start = DateTime.Now });
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntries(0, 10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTimeEntries_CountIsTotalBeforePagination()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.TimeEntries.AddRange(Enumerable.Range(0, 5)
            .Select(_ => new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now }));
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntries(skip: 2, limit: 2);

        Assert.Equal(2, result.TimeEntries.Count);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetTimeEntries_ExcludesEntriesOnSoftDeletedProjects()
    {
        using var context = CreateContext();
        var activeProject = MakeProject(UserId, "Active");
        var deletedProject = MakeProject(UserId, "Deleted", isDeleted: true);
        context.Projects.AddRange(activeProject, deletedProject);
        await context.SaveChangesAsync();

        context.TimeEntries.AddRange(
            new TimeEntry { ProjectId = activeProject.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = deletedProject.Id, UserId = UserId, Start = DateTime.Now });
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntries(0, 10);

        Assert.Equal(1, result.Count);
    }

    [Fact]
    public async Task GetTimeEntriesByProjectId_ExcludesEntriesOnSoftDeletedProject()
    {
        using var context = CreateContext();
        var deletedProject = MakeProject(UserId, isDeleted: true);
        context.Projects.Add(deletedProject);
        await context.SaveChangesAsync();

        context.TimeEntries.Add(
            new TimeEntry { ProjectId = deletedProject.Id, UserId = UserId, Start = DateTime.Now });
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntriesByProjectId(deletedProject.Id, 0, 10);

        Assert.Equal(0, result.Count);
    }

    [Fact]
    public async Task GetTimeEntriesByYear_FiltersCorrectly()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 3, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 9, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2023, 6, 1) });
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntriesByYear(2024, 0, 10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTimeEntriesByMonth_FiltersCorrectly()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 15) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 7, 1) });
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntriesByMonth(6, 2024, 0, 10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTimeEntriesByDay_FiltersCorrectly()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 15, 9, 0, 0) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 15, 14, 0, 0) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 16, 9, 0, 0) });
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntriesByDay(15, 6, 2024, 0, 10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTimeEntriesByProjectId_FiltersCorrectly()
    {
        using var context = CreateContext();
        var projectA = MakeProject(UserId, "A");
        var projectB = MakeProject(UserId, "B");
        context.Projects.AddRange(projectA, projectB);
        await context.SaveChangesAsync();

        context.TimeEntries.AddRange(
            new TimeEntry { ProjectId = projectA.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = projectA.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = projectB.Id, UserId = UserId, Start = DateTime.Now });
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntriesByProjectId(projectA.Id, 0, 10);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTimeEntryById_ReturnsEntry_ForCurrentUser()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var entry = new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now };
        context.TimeEntries.Add(entry);
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntryById(entry.Id);

        Assert.NotNull(result);
        Assert.Equal(entry.Id, result.Id);
    }

    [Fact]
    public async Task GetTimeEntryById_ReturnsNull_ForOtherUsersEntry()
    {
        using var context = CreateContext();
        var project = MakeProject(OtherUserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var entry = new TimeEntry { ProjectId = project.Id, UserId = OtherUserId, Start = DateTime.Now };
        context.TimeEntries.Add(entry);
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntryById(entry.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTimeEntryById_ReturnsNull_WhenNotFound()
    {
        using var context = CreateContext();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntryById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTimeEntry_SetsUserIdFromContext()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        await new TimeEntryService(context, new FakeUserContextService(UserId))
            .CreateTimeEntry(new TimeEntryCreateRequest { ProjectId = project.Id, Start = DateTime.Now });

        var entry = context.TimeEntries.Single();
        Assert.Equal(UserId, entry.UserId);
    }

    [Fact]
    public async Task UpdateTimeEntry_UpdatesFields()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var entry = new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now };
        context.TimeEntries.Add(entry);
        await context.SaveChangesAsync();

        var newStart = new DateTime(2024, 6, 1, 9, 0, 0);
        var newEnd = new DateTime(2024, 6, 1, 10, 0, 0);

        await new TimeEntryService(context, new FakeUserContextService(UserId))
            .UpdateTimeEntry(entry.Id, new TimeEntryUpdateRequest
            {
                ProjectId = project.Id,
                Start = newStart,
                End = newEnd
            });

        var updated = context.TimeEntries.Single();
        Assert.Equal(newStart, updated.Start);
        Assert.Equal(newEnd, updated.End);
    }

    [Fact]
    public async Task UpdateTimeEntry_ThrowsEntityNotFoundException_WhenNotFound()
    {
        using var context = CreateContext();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            new TimeEntryService(context, new FakeUserContextService(UserId))
                .UpdateTimeEntry(999, new TimeEntryUpdateRequest { Start = DateTime.Now }));
    }

    [Fact]
    public async Task DeleteTimeEntry_RemovesEntry()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var entry = new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now };
        context.TimeEntries.Add(entry);
        await context.SaveChangesAsync();

        await new TimeEntryService(context, new FakeUserContextService(UserId))
            .DeleteTimeEntry(entry.Id);

        Assert.Empty(context.TimeEntries);
    }

    [Fact]
    public async Task DeleteTimeEntry_ThrowsEntityNotFoundException_WhenNotFound()
    {
        using var context = CreateContext();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            new TimeEntryService(context, new FakeUserContextService(UserId))
                .DeleteTimeEntry(999));
    }

    [Fact]
    public async Task GetAllTimeEntriesByYear_ReturnsAllWithoutPagination()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 1, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 12, 31) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2023, 1, 1) });
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetAllTimeEntriesByYear(2024);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task TotalDuration_IsCalculatedCorrectly()
    {
        using var context = CreateContext();
        var project = MakeProject(UserId);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        context.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 1, 1, 9, 0, 0), End = new DateTime(2024, 1, 1, 11, 0, 0) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 1, 2, 9, 0, 0), End = new DateTime(2024, 1, 2, 9, 30, 0) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now });
        await context.SaveChangesAsync();

        var result = await new TimeEntryService(context, new FakeUserContextService(UserId))
            .GetTimeEntries(0, 10);

        Assert.Equal(TimeSpan.FromHours(2.5), result.TotalDuration);
    }
}
