using TimeTracker.Client.Features.TimeEntries.Services;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.ComponentTests.Features.TimeEntries;

/// <summary>
/// Regression tests for #430 — entries started before 11:00 AEDT (UTC+11) were shown under
/// the previous day because filtering used the UTC date. The zone is passed explicitly so
/// the tests are meaningful regardless of the machine's local zone (CI runs in UTC).
/// </summary>
public class TimeEntryFiltersTests
{
    private static readonly TimeZoneInfo Sydney = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");

    // 09:30 AEDT on Sat 17 Oct 2026 = 22:30Z on Fri 16 Oct
    private static readonly DateTime MorningUtc = new(2026, 10, 16, 22, 30, 0, DateTimeKind.Utc);
    // 08:00 AEDT on 1 Jan 2027 = 21:00Z on 31 Dec 2026
    private static readonly DateTime NewYearMorningUtc = new(2026, 12, 31, 21, 0, 0, DateTimeKind.Utc);

    private static TimeEntryResponse MakeEntry(int id, DateTime startUtc) =>
        new(id, new ProjectSummary(1, "Acme"), startUtc, startUtc.AddHours(1), null, null, null, null, false);

    [Fact]
    public void ForDay_IncludesEntryOnLocalDay_WhenUtcDateIsPreviousDay()
    {
        var entries = new[] { MakeEntry(1, MorningUtc) };

        var result = TimeEntryFilters.ForDay(entries, new DateTime(2026, 10, 17), Sydney);

        Assert.Single(result);
    }

    [Fact]
    public void ForDay_ExcludesEntryFromPreviousLocalDay_WhenUtcDateMatches()
    {
        var entries = new[] { MakeEntry(1, MorningUtc) };

        var result = TimeEntryFilters.ForDay(entries, new DateTime(2026, 10, 16), Sydney);

        Assert.Empty(result);
    }

    [Fact]
    public void GroupByDay_KeysByLocalDate()
    {
        var entries = new[] { MakeEntry(1, MorningUtc) };

        var group = Assert.Single(TimeEntryFilters.GroupByDay(entries, Sydney));

        Assert.Equal(new DateTime(2026, 10, 17), group.Key);
    }

    [Fact]
    public void ForMonth_UsesLocalMonth_AtYearBoundary()
    {
        var entries = new[] { MakeEntry(1, NewYearMorningUtc) };

        Assert.Single(TimeEntryFilters.ForMonth(entries, 2027, 1, Sydney));
        Assert.Empty(TimeEntryFilters.ForMonth(entries, 2026, 12, Sydney));
    }

    [Fact]
    public void ForYear_UsesLocalYear_AtYearBoundary()
    {
        var entries = new[] { MakeEntry(1, NewYearMorningUtc) };

        Assert.Single(TimeEntryFilters.ForYear(entries, 2027, Sydney));
        Assert.Empty(TimeEntryFilters.ForYear(entries, 2026, Sydney));
    }
}
