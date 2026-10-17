namespace TimeTracker.Web.Features.TimeEntries;

/// <summary>
/// A user's local calendar period (a day or a year) expressed as a half-open UTC range
/// [FromUtc, ToUtc). Time entries are stored in UTC and the server runs in UTC, so "today"
/// or "2026" only has meaning relative to the caller's time zone. See ADR-038.
/// </summary>
public readonly record struct LocalDateRange(DateTime FromUtc, DateTime ToUtc)
{
    /// <exception cref="ArgumentException">The id is not a known IANA/Windows time zone.</exception>
    public static TimeZoneInfo ResolveTimeZone(string timeZoneId) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone)
            ? timeZone
            : throw new ArgumentException($"Unknown time zone '{timeZoneId}'.", nameof(timeZoneId));

    public static LocalDateRange Today(TimeZoneInfo timeZone, DateTime utcNow) =>
        ForDay(TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone).Date, timeZone);

    public static LocalDateRange ForDay(DateTime localDate, TimeZoneInfo timeZone) =>
        new(StartOfLocalDayUtc(localDate.Date, timeZone),
            StartOfLocalDayUtc(localDate.Date.AddDays(1), timeZone));

    public static LocalDateRange ForYear(int year, TimeZoneInfo timeZone) =>
        new(StartOfLocalDayUtc(new DateTime(year, 1, 1), timeZone),
            StartOfLocalDayUtc(new DateTime(year + 1, 1, 1), timeZone));

    private static DateTime StartOfLocalDayUtc(DateTime localDate, TimeZoneInfo timeZone)
    {
        var local = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);

        // Some zones start DST at midnight (e.g. America/Santiago), so local 00:00 never
        // happens — the day begins at the first valid local time instead.
        while (timeZone.IsInvalidTime(local))
            local = local.AddMinutes(30);

        // If DST ends at midnight, 00:00 happens twice — the day begins at the first one.
        if (timeZone.IsAmbiguousTime(local))
            return DateTime.SpecifyKind(local - timeZone.GetAmbiguousTimeOffsets(local).Max(), DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }
}
