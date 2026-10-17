using TimeTracker.Client.Shared;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Client.Features.TimeEntries.Services;

/// <summary>
/// Date-based filtering and grouping for the entries listing. All comparisons use the
/// entry's local start time — see <see cref="LocalDates"/>.
/// </summary>
public static class TimeEntryFilters
{
    public static IEnumerable<TimeEntryResponse> ForDay(
        IEnumerable<TimeEntryResponse> entries, DateTime day, TimeZoneInfo? timeZone = null) =>
        entries.Where(e => e.Start.ToLocal(timeZone).Date == day.Date);

    public static IEnumerable<TimeEntryResponse> ForMonth(
        IEnumerable<TimeEntryResponse> entries, int year, int month, TimeZoneInfo? timeZone = null) =>
        entries.Where(e =>
        {
            var local = e.Start.ToLocal(timeZone);
            return local.Year == year && local.Month == month;
        });

    public static IEnumerable<TimeEntryResponse> ForYear(
        IEnumerable<TimeEntryResponse> entries, int year, TimeZoneInfo? timeZone = null) =>
        entries.Where(e => e.Start.ToLocal(timeZone).Year == year);

    public static IEnumerable<IGrouping<DateTime, TimeEntryResponse>> GroupByDay(
        IEnumerable<TimeEntryResponse> entries, TimeZoneInfo? timeZone = null) =>
        entries.GroupBy(e => e.Start.ToLocal(timeZone).Date);
}
