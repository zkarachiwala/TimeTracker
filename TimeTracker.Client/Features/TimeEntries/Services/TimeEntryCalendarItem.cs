using Heron.MudCalendar;

namespace TimeTracker.Client.Features.TimeEntries.Services;

public class TimeEntryCalendarItem : CalendarItem
{
    public int EntryId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? Note { get; set; }

    public string DurationDisplay
    {
        get
        {
            if (!End.HasValue) return "running";
            var ts = End.Value - Start;
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                : $"{ts.Minutes}m";
        }
    }

    public string TimeDisplay => End.HasValue
        ? $"{Start:t} — {End.Value:t}"
        : $"{Start:t} — now";
}
