namespace TimeTracker.Client.Shared;

/// <summary>
/// Time entries arrive from the API as UTC. Any date-part logic (which day, month or year
/// an entry belongs to) must be done in the user's local zone, otherwise entries near
/// midnight UTC land on the wrong day (e.g. 09:30 AEDT is 22:30Z the previous day).
/// </summary>
public static class LocalDates
{
    /// <summary>Converts to the given zone, defaulting to the browser's local zone.</summary>
    public static DateTime ToLocal(this DateTime value, TimeZoneInfo? timeZone = null) =>
        TimeZoneInfo.ConvertTime(value, timeZone ?? TimeZoneInfo.Local);
}
