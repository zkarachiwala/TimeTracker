using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using TimeTracker.Web.Data;
using TimeTracker.Web.Infrastructure;
using Xunit;

namespace TimeTracker.Tests.Infrastructure;

/// <summary>
/// The production database auto-pauses (Azure SQL free offer, serverless) and returns error 40613
/// on the first connection while it resumes. EF Core's default strategy does not retry, which is
/// what failed the deploy pipeline's migrate job. These tests pin both halves of the fix: that a
/// retrying strategy is actually configured, and that its budget stays bounded. See ADR-036.
///
/// No database is touched - configuring options and asking for the execution strategy never opens
/// a connection - so these run in the fast loop.
/// </summary>
public class SqlServerRetryPolicyTests
{
    private const string DummyConnection = "Server=localhost;Database=TimeTrackerDb;Encrypt=False";

    private static TimeTrackerDataContext CreateContext(bool withRetryPolicy)
    {
        var builder = new DbContextOptionsBuilder<TimeTrackerDataContext>();

        if (withRetryPolicy)
            builder.UseSqlServer(DummyConnection, SqlServerRetryPolicy.Apply);
        else
            builder.UseSqlServer(DummyConnection);

        return new TimeTrackerDataContext(builder.Options);
    }

    [Fact]
    public void Apply_ConfiguresRetryingExecutionStrategy()
    {
        using var context = CreateContext(withRetryPolicy: true);

        var strategy = context.Database.CreateExecutionStrategy();

        Assert.IsType<SqlServerRetryingExecutionStrategy>(strategy);
        Assert.True(strategy.RetriesOnFailure);
    }

    /// <summary>
    /// Guards the assertion above against silently passing: without the policy EF Core uses the
    /// non-retrying default, which is the state that let 40613 fail the pipeline outright.
    /// </summary>
    [Fact]
    public void WithoutPolicy_UsesNonRetryingDefault()
    {
        using var context = CreateContext(withRetryPolicy: false);

        var strategy = context.Database.CreateExecutionStrategy();

        Assert.False(strategy.RetriesOnFailure);
    }

    /// <summary>
    /// The free offer's exhaustion behaviour is AutoPause: if the monthly vCore allowance runs out
    /// the database is unreachable until the next calendar month, and retrying cannot fix that. The
    /// budget must cover a resume and then stop.
    /// </summary>
    [Fact]
    public void RetryBudget_IsBoundedAndCoversAResume()
    {
        Assert.InRange(SqlServerRetryPolicy.MaxRetryCount, 1, 10);
        Assert.InRange(SqlServerRetryPolicy.MaxRetryDelay, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));

        // EF Core's backoff is (2^n - 1) seconds, capped at MaxRetryDelay, summed over the attempts.
        // A serverless resume takes roughly 60-90 seconds, and Azure App Service cuts a request off
        // at 230 seconds, so the total has to sit between the two.
        var worstCase = TimeSpan.Zero;
        for (var attempt = 1; attempt <= SqlServerRetryPolicy.MaxRetryCount; attempt++)
        {
            var uncapped = TimeSpan.FromSeconds(Math.Pow(2, attempt) - 1);
            worstCase += uncapped < SqlServerRetryPolicy.MaxRetryDelay
                ? uncapped
                : SqlServerRetryPolicy.MaxRetryDelay;
        }

        Assert.InRange(worstCase, TimeSpan.FromSeconds(90), TimeSpan.FromSeconds(200));
    }
}
