using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;

namespace TimeTracker.Tests.Infrastructure;

/// <summary>
/// Creates fresh context instances from shared options so services can dispose their
/// own context without affecting the test's context. Both share the same in-memory DB.
/// </summary>
public sealed class TestDbContextFactory(DbContextOptions<TimeTrackerDataContext> options)
    : IDbContextFactory<TimeTrackerDataContext>
{
    public TimeTrackerDataContext CreateDbContext() => new(options);
    public Task<TimeTrackerDataContext> CreateDbContextAsync(CancellationToken _ = default)
        => Task.FromResult(new TimeTrackerDataContext(options));
}
