using TimeTracker.Web.Features.TimeEntries;
using Xunit;

namespace TimeTracker.Tests.Features.TimeEntries;

public class LocalDateRangeTests
{
    private static readonly TimeZoneInfo Sydney = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");

    private static DateTime Utc(int y, int mo, int d, int h = 0, int mi = 0) => new(y, mo, d, h, mi, 0, DateTimeKind.Utc);

    [Fact]
    public void ForDay_ReturnsLocalMidnightToMidnight_AsUtc()
    {
        // AEDT (UTC+11): 17 Oct 00:00 local = 16 Oct 13:00Z
        var range = LocalDateRange.ForDay(new DateTime(2026, 10, 17), Sydney);

        Assert.Equal(Utc(2026, 10, 16, 13), range.FromUtc);
        Assert.Equal(Utc(2026, 10, 17, 13), range.ToUtc);
    }

    [Fact]
    public void ForDay_Is23Hours_OnDstStartDay()
    {
        // Sydney DST starts 02:00 on 4 Oct 2026: midnight is AEST (+10), next midnight is AEDT (+11)
        var range = LocalDateRange.ForDay(new DateTime(2026, 10, 4), Sydney);

        Assert.Equal(Utc(2026, 10, 3, 14), range.FromUtc);
        Assert.Equal(Utc(2026, 10, 4, 13), range.ToUtc);
    }

    [Fact]
    public void ForDay_StartsAtFirstValidTime_WhenMidnightIsSkipped()
    {
        // Chile starts DST at 24:00 on the first Saturday of September — local 00:00 on
        // Sun 6 Sep 2026 doesn't exist; clocks jump from 23:59:59 (-04) to 01:00 (-03) = 04:00Z.
        var santiago = TimeZoneInfo.FindSystemTimeZoneById("America/Santiago");

        var range = LocalDateRange.ForDay(new DateTime(2026, 9, 6), santiago);

        Assert.Equal(Utc(2026, 9, 6, 4), range.FromUtc);
    }

    [Fact]
    public void ForYear_ReturnsLocalNewYearToNewYear_AsUtc()
    {
        var range = LocalDateRange.ForYear(2027, Sydney);

        Assert.Equal(Utc(2026, 12, 31, 13), range.FromUtc);
        Assert.Equal(Utc(2027, 12, 31, 13), range.ToUtc);
    }

    [Fact]
    public void Today_UsesLocalDate_NotUtcDate()
    {
        // 22:30Z on 16 Oct is 09:30 on 17 Oct in Sydney
        var range = LocalDateRange.Today(Sydney, Utc(2026, 10, 16, 22, 30));

        Assert.Equal(Utc(2026, 10, 16, 13), range.FromUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Not/AZone")]
    public void ResolveTimeZone_Throws_ForUnknownId(string id) =>
        Assert.Throws<ArgumentException>(() => LocalDateRange.ResolveTimeZone(id));

    [Fact]
    public void ResolveTimeZone_AcceptsIanaId() =>
        Assert.Equal(Sydney.BaseUtcOffset, LocalDateRange.ResolveTimeZone("Australia/Sydney").BaseUtcOffset);
}
