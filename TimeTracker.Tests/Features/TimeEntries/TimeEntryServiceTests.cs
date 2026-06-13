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

    private static DbContextOptions<TimeTrackerDataContext> CreateOptions() =>
        new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static TimeEntryService CreateService(DbContextOptions<TimeTrackerDataContext> options, string userId = UserId) =>
        new(new TestDbContextFactory(options), new FakeUserContextService(userId));

    private static Project MakeProject(string userId, string name = "Project", bool isDeleted = false) =>
        new() { Name = name, IsDeleted = isDeleted, ProjectUsers = [new ProjectUser { UserId = userId }] };

    [Fact]
    public async Task GetTimeEntries_ReturnsOnlyCurrentUserEntries()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = project.Id, UserId = OtherUserId, Start = DateTime.Now });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntries(0, 10);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTimeEntries_CountIsTotalBeforePagination()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(Enumerable.Range(0, 5)
            .Select(_ => new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now }));
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntries(skip: 2, limit: 2);
        Assert.Equal(2, result.TimeEntries.Count);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetTimeEntries_ExcludesEntriesOnSoftDeletedProjects()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var activeProject = MakeProject(UserId, "Active");
        var deletedProject = MakeProject(UserId, "Deleted", isDeleted: true);
        seed.Projects.AddRange(activeProject, deletedProject);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = activeProject.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = deletedProject.Id, UserId = UserId, Start = DateTime.Now });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntries(0, 10);
        Assert.Equal(1, result.Count);
    }

    [Fact]
    public async Task GetTimeEntriesByProjectId_ExcludesEntriesOnSoftDeletedProject()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var deletedProject = MakeProject(UserId, isDeleted: true);
        seed.Projects.Add(deletedProject);
        await seed.SaveChangesAsync();
        seed.TimeEntries.Add(new TimeEntry { ProjectId = deletedProject.Id, UserId = UserId, Start = DateTime.Now });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntriesByProjectId(deletedProject.Id, 0, 10);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public async Task GetTimeEntriesByYear_FiltersCorrectly()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 3, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 9, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2023, 6, 1) });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntriesByYear(2024, 0, 10);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTimeEntriesByMonth_FiltersCorrectly()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 15) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 7, 1) });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntriesByMonth(6, 2024, 0, 10);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTimeEntriesByDay_FiltersCorrectly()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 15, 9, 0, 0) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 15, 14, 0, 0) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 16, 9, 0, 0) });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntriesByDay(15, 6, 2024, 0, 10);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTimeEntriesByProjectId_FiltersCorrectly()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var projectA = MakeProject(UserId, "A");
        var projectB = MakeProject(UserId, "B");
        seed.Projects.AddRange(projectA, projectB);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = projectA.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = projectA.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = projectB.Id, UserId = UserId, Start = DateTime.Now });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntriesByProjectId(projectA.Id, 0, 10);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetTimeEntryById_ReturnsEntry_ForCurrentUser()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        var entry = new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now };
        seed.TimeEntries.Add(entry);
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntryById(entry.Id);
        Assert.NotNull(result);
        Assert.Equal(entry.Id, result.Id);
    }

    [Fact]
    public async Task GetTimeEntryById_ReturnsNull_ForOtherUsersEntry()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(OtherUserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        var entry = new TimeEntry { ProjectId = project.Id, UserId = OtherUserId, Start = DateTime.Now };
        seed.TimeEntries.Add(entry);
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntryById(entry.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTimeEntryById_ReturnsNull_WhenNotFound()
    {
        var options = CreateOptions();
        var result = await CreateService(options).GetTimeEntryById(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTimeEntry_SetsUserIdFromContext()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();

        await CreateService(options).CreateTimeEntry(new TimeEntryCreateRequest { ProjectId = project.Id, Start = DateTime.Now });

        using var context = new TimeTrackerDataContext(options);
        Assert.Equal(UserId, context.TimeEntries.Single().UserId);
    }

    [Fact]
    public async Task UpdateTimeEntry_UpdatesFields()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        var entry = new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now };
        seed.TimeEntries.Add(entry);
        await seed.SaveChangesAsync();

        var newStart = new DateTime(2024, 6, 1, 9, 0, 0);
        var newEnd = new DateTime(2024, 6, 1, 10, 0, 0);
        await CreateService(options).UpdateTimeEntry(entry.Id, new TimeEntryUpdateRequest { ProjectId = project.Id, Start = newStart, End = newEnd });

        using var context = new TimeTrackerDataContext(options);
        var updated = context.TimeEntries.Single();
        Assert.Equal(newStart, updated.Start);
        Assert.Equal(newEnd, updated.End);
    }

    [Fact]
    public async Task UpdateTimeEntry_ThrowsEntityNotFoundException_WhenNotFound()
    {
        var options = CreateOptions();
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            CreateService(options).UpdateTimeEntry(999, new TimeEntryUpdateRequest { Start = DateTime.Now }));
    }

    [Fact]
    public async Task DeleteTimeEntry_RemovesEntry()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        var entry = new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now };
        seed.TimeEntries.Add(entry);
        await seed.SaveChangesAsync();

        await CreateService(options).DeleteTimeEntry(entry.Id);

        using var context = new TimeTrackerDataContext(options);
        Assert.Empty(context.TimeEntries);
    }

    [Fact]
    public async Task DeleteTimeEntry_ThrowsEntityNotFoundException_WhenNotFound()
    {
        var options = CreateOptions();
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            CreateService(options).DeleteTimeEntry(999));
    }

    [Fact]
    public async Task GetAllTimeEntriesByYear_ReturnsAllWithoutPagination()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 1, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 6, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 12, 31) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2023, 1, 1) });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetAllTimeEntriesByYear(2024);
        Assert.Equal(3, result.Count);
    }

    // --- UpdateTimeEntry invoice fields ---

    [Fact]
    public async Task UpdateTimeEntry_PersistsInvoiceReference()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        var entry = new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now };
        seed.TimeEntries.Add(entry);
        await seed.SaveChangesAsync();

        var invoicedAt = new DateTime(2026, 6, 1);
        await CreateService(options).UpdateTimeEntry(entry.Id, new TimeEntryUpdateRequest
        {
            ProjectId = project.Id,
            Start = entry.Start,
            InvoiceReference = "INV-0042",
            InvoicedAt = invoicedAt
        });

        using var context = new TimeTrackerDataContext(options);
        var updated = context.TimeEntries.Single();
        Assert.Equal("INV-0042", updated.InvoiceReference);
        Assert.Equal(invoicedAt, updated.InvoicedAt);
    }

    [Fact]
    public async Task UpdateTimeEntry_ClearsInvoiceReference_WhenSetToNull()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        var entry = new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now, InvoiceReference = "INV-0001", InvoicedAt = DateTime.Today };
        seed.TimeEntries.Add(entry);
        await seed.SaveChangesAsync();

        await CreateService(options).UpdateTimeEntry(entry.Id, new TimeEntryUpdateRequest
        {
            ProjectId = project.Id,
            Start = entry.Start,
            InvoiceReference = null,
            InvoicedAt = null
        });

        using var context = new TimeTrackerDataContext(options);
        var updated = context.TimeEntries.Single();
        Assert.Null(updated.InvoiceReference);
        Assert.Null(updated.InvoicedAt);
    }

    // --- GetActiveTimeEntry ---

    [Fact]
    public async Task GetActiveTimeEntry_ReturnsRunningEntry()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        var active = new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now, End = null };
        var finished = new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now.AddHours(-2), End = DateTime.Now.AddHours(-1) };
        seed.TimeEntries.AddRange(active, finished);
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetActiveTimeEntry();

        Assert.NotNull(result);
        Assert.Equal(active.Id, result.Id);
    }

    [Fact]
    public async Task GetActiveTimeEntry_ReturnsNull_WhenNoneRunning()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.Add(new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now.AddHours(-1), End = DateTime.Now });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetActiveTimeEntry();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveTimeEntry_ReturnsNull_ForOtherUsersRunningEntry()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(OtherUserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.Add(new TimeEntry { ProjectId = project.Id, UserId = OtherUserId, Start = DateTime.Now, End = null });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetActiveTimeEntry();

        Assert.Null(result);
    }

    // --- GetTodaysTimeEntries ---

    [Fact]
    public async Task GetTodaysTimeEntries_ReturnsOnlyTodaysEntries()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Today.AddHours(9) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Today.AddHours(14) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Today.AddDays(-1).AddHours(9) });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTodaysTimeEntries();

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(DateTime.Today, e.Start.Date));
    }

    [Fact]
    public async Task GetTodaysTimeEntries_ReturnsOrderedByStartDescending()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Today.AddHours(9) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Today.AddHours(14) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Today.AddHours(11) });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTodaysTimeEntries();

        Assert.Equal(14, result[0].Start.Hour);
        Assert.Equal(11, result[1].Start.Hour);
        Assert.Equal(9, result[2].Start.Hour);
    }

    // --- GetAllTimeEntriesByProject ---

    [Fact]
    public async Task GetAllTimeEntriesByProject_ReturnsOnlyProjectEntries()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var projectA = MakeProject(UserId, "A");
        var projectB = MakeProject(UserId, "B");
        seed.Projects.AddRange(projectA, projectB);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = projectA.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = projectA.Id, UserId = UserId, Start = DateTime.Now },
            new TimeEntry { ProjectId = projectB.Id, UserId = UserId, Start = DateTime.Now });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetAllTimeEntriesByProject(projectA.Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(projectA.Id, e.Project.Id));
    }

    [Fact]
    public async Task GetAllTimeEntriesByProject_ReturnsOrderedByStartDescending()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 1, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 3, 1) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 2, 1) });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetAllTimeEntriesByProject(project.Id);

        Assert.Equal(new DateTime(2024, 3, 1), result[0].Start);
        Assert.Equal(new DateTime(2024, 1, 1), result[2].Start);
    }

    // --- TotalDuration ---

    [Fact]
    public async Task TotalDuration_IsCalculatedCorrectly()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 1, 1, 9, 0, 0), End = new DateTime(2024, 1, 1, 11, 0, 0) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 1, 2, 9, 0, 0), End = new DateTime(2024, 1, 2, 9, 30, 0) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntries(0, 10);
        Assert.Equal(TimeSpan.FromHours(2.5), result.TotalDuration);
    }

    [Fact]
    public async Task TotalDuration_ReflectsAllEntries_NotJustCurrentPage()
    {
        var options = CreateOptions();
        using var seed = new TimeTrackerDataContext(options);
        var project = MakeProject(UserId);
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        seed.TimeEntries.AddRange(
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 1, 1, 9, 0, 0), End = new DateTime(2024, 1, 1, 11, 0, 0) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 1, 2, 9, 0, 0), End = new DateTime(2024, 1, 2, 10, 0, 0) },
            new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = new DateTime(2024, 1, 3, 9, 0, 0), End = new DateTime(2024, 1, 3, 10, 30, 0) });
        await seed.SaveChangesAsync();

        // Request only the first page (1 entry) — TotalDuration must still sum all 3
        var result = await CreateService(options).GetTimeEntries(skip: 0, limit: 1);
        Assert.Single(result.TimeEntries);
        Assert.Equal(3, result.Count);
        Assert.Equal(TimeSpan.FromHours(4.5), result.TotalDuration);
    }

    [Fact]
    public async Task GetTimeEntries_LimitIsCappedAt200()
    {
        var options = CreateOptions();
        await using var seed = new TimeTrackerDataContext(options);
        var project = new Project { Name = "P", ProjectUsers = [new ProjectUser { UserId = UserId }] };
        seed.Projects.Add(project);
        await seed.SaveChangesAsync();
        for (var i = 0; i < 205; i++)
            seed.TimeEntries.Add(new TimeEntry { ProjectId = project.Id, UserId = UserId, Start = DateTime.Now.AddMinutes(-i), End = DateTime.Now.AddMinutes(-i + 1) });
        await seed.SaveChangesAsync();

        var result = await CreateService(options).GetTimeEntries(skip: 0, limit: 999);
        Assert.Equal(200, result.TimeEntries.Count);
    }
}
