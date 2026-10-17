using TimeTracker.Client.Features.TimeEntries.Services;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.ComponentTests.Features.TimeEntries;

/// <summary>
/// Regression tests for #430 — calendar totals and tooltips must bucket entries by the
/// user's local date and show local times. Zone passed explicitly; see TimeEntryFiltersTests.
/// </summary>
public class CalendarDataTransformerTests
{
    private static readonly TimeZoneInfo Sydney = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney");

    // 09:30–10:30 AEDT on 17 Oct 2026 = 22:30Z–23:30Z on 16 Oct
    private static readonly DateTime StartUtc = new(2026, 10, 16, 22, 30, 0, DateTimeKind.Utc);

    private static List<TimeEntryResponse> Entries() =>
        [new(1, new ProjectSummary(1, "Acme"), StartUtc, StartUtc.AddHours(1), null, null, null, null, false)];

    [Fact]
    public void ToDailyProjectValues_UsesLocalDate()
    {
        var value = Assert.Single(CalendarDataTransformer.ToDailyProjectValues(Entries(), Sydney));

        Assert.Equal(new DateTime(2026, 10, 17), value.Date);
    }

    [Fact]
    public void ToCalendarItemsForDay_UsesLocalDate()
    {
        Assert.Single(CalendarDataTransformer.ToCalendarItemsForDay(Entries(), new DateTime(2026, 10, 17), Sydney));
        Assert.Empty(CalendarDataTransformer.ToCalendarItemsForDay(Entries(), new DateTime(2026, 10, 16), Sydney));
    }

    [Fact]
    public void ToTooltipLookup_KeysByLocalDate_AndShowsLocalTimes()
    {
        var lookup = CalendarDataTransformer.ToTooltipLookup(Entries(), timeZone: Sydney);

        var data = lookup[(new DateTime(2026, 10, 17), "Acme")];
        var expected = $"{new DateTime(2026, 10, 17, 9, 30, 0):t} — {new DateTime(2026, 10, 17, 10, 30, 0):t}";
        Assert.Equal(expected, Assert.Single(data.Entries).TimeRange);
    }
}
