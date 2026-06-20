using Heron.MudCalendar;
using Heron.MudTotalCalendar;
using TimeTracker.Client.Shared;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Client.Features.TimeEntries.Services;

public static class CalendarDataTransformer
{
    public static List<TimeEntryCalendarItem> ToCalendarItems(List<TimeEntryResponse> entries)
    {
        return entries.Select(e => new TimeEntryCalendarItem
        {
            EntryId = e.Id,
            ProjectId = e.Project.Id,
            Start = e.Start,
            End = e.End,
            Text = e.Project.Name,
            ProjectName = e.Project.Name,
            Note = e.Note
        }).ToList();
    }

    public static List<Value> ToDailyProjectValues(List<TimeEntryResponse> entries)
    {
        var definitions = new Dictionary<int, ValueDefinition>();
        var values = new List<Value>();

        foreach (var group in entries
            .Where(e => e.End.HasValue)
            .GroupBy(e => e.Start.Date))
        {
            var date = group.Key;

            foreach (var projectGroup in group.GroupBy(e => e.Project.Id))
            {
                if (!definitions.TryGetValue(projectGroup.Key, out var def))
                {
                    var projectName = projectGroup.First().Project.Name;
                    var color = ProjectColors.ForProject(projectGroup.Key);
                    def = new ValueDefinition
                    {
                        Name = projectName,
                        Units = "",
                        FormatString = "",
                        FormatFunc = seconds =>
                        {
                            var ts = TimeSpan.FromSeconds(seconds);
                            return ts.TotalHours >= 1
                                ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                                : $"{ts.Minutes}m";
                        },
                        Style = $"background-color:{color};color:#fff;border-radius:3px;padding:0 4px;font-size:12px"
                    };
                    definitions[projectGroup.Key] = def;
                }

                var totalSeconds = projectGroup.Sum(e => (e.End!.Value - e.Start).TotalSeconds);

                values.Add(new Value
                {
                    Date = date,
                    Definition = def,
                    Amount = totalSeconds
                });
            }
        }

        return values;
    }

    public static List<TimeEntryCalendarItem> ToCalendarItemsForDay(List<TimeEntryResponse> entries, DateTime date)
    {
        return entries
            .Where(e => e.Start.Date == date.Date)
            .Select(e => new TimeEntryCalendarItem
            {
                EntryId = e.Id,
                ProjectId = e.Project.Id,
                Start = e.Start,
                End = e.End,
                Text = e.Project.Name,
                ProjectName = e.Project.Name,
                Note = e.Note
            }).ToList();
    }

    public static Dictionary<(DateTime Date, string ProjectName), ValueTooltipData> ToTooltipLookup(
        List<TimeEntryResponse> entries,
        Dictionary<int, string>? projectClientNames = null)
    {
        var lookup = new Dictionary<(DateTime, string), ValueTooltipData>();

        foreach (var entry in entries.Where(e => e.End.HasValue))
        {
            var key = (entry.Start.Date, entry.Project.Name);
            if (!lookup.TryGetValue(key, out var data))
            {
                var clientName = projectClientNames?.GetValueOrDefault(entry.Project.Id);
                data = new ValueTooltipData
                {
                    ProjectId = entry.Project.Id,
                    ProjectName = entry.Project.Name,
                    ClientName = clientName
                };
                lookup[key] = data;
            }

            var ts = entry.End!.Value - entry.Start;
            data.Entries.Add(new ValueTooltipData.EntryBreakdown
            {
                TimeRange = $"{entry.Start:t} — {entry.End.Value:t}",
                Duration = ts.TotalHours >= 1
                    ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                    : $"{ts.Minutes}m"
            });
        }

        return lookup;
    }
}
