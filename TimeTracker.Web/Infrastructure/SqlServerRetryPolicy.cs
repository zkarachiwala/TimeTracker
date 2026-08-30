using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TimeTracker.Web.Infrastructure;

/// <summary>
/// Connection resiliency for Azure SQL serverless auto-pause.
///
/// The production database runs on the Azure SQL free offer (serverless, General Purpose) and
/// auto-pauses when idle. The first connection after a pause returns error 40613 - "Database is
/// not currently available" - while compute resumes, which typically takes 60-90 seconds. EF
/// Core's default SqlServerExecutionStrategy performs no retries at all, so that first connection
/// simply fails. Enabling retry swaps in SqlServerRetryingExecutionStrategy, which already treats
/// 40613 as transient.
///
/// The retry budget is deliberately bounded rather than generous. Retrying does not itself consume
/// the free tier's vCore allowance - the database is resuming regardless, and the resume is a cost
/// already paid the moment anything connects - but an open-ended retry loop is still wrong here:
/// if the monthly 100,000 vCore-second allowance is exhausted, the database stays paused until the
/// start of the next calendar month (the free-limit exhaustion behaviour is AutoPause) and no
/// amount of retrying can bring it back. Retries must cover a resume, then give up.
///
/// See ADR-036.
/// </summary>
public static class SqlServerRetryPolicy
{
    /// <summary>
    /// Chosen with <see cref="MaxRetryDelay"/> so the total delay budget is roughly 105 seconds -
    /// enough to cover the upper end of a serverless resume, and comfortably inside Azure App
    /// Service's 230-second request timeout so a waking request completes rather than being cut off.
    /// </summary>
    public const int MaxRetryCount = 8;

    /// <summary>
    /// Caps EF Core's exponential backoff. Delays run roughly 1s, 3s, 7s, 15s, then 20s per
    /// remaining attempt, totalling ~105 seconds before the strategy gives up.
    /// </summary>
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Applies the policy to a SQL Server provider. Pass as the options callback to UseSqlServer
    /// so every context - the running app and EF Core's design-time tooling alike - shares one
    /// definition.
    /// </summary>
    public static void Apply(SqlServerDbContextOptionsBuilder options) =>
        options.EnableRetryOnFailure(MaxRetryCount, MaxRetryDelay, errorNumbersToAdd: null);
}
