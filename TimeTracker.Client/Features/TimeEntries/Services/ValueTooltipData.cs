namespace TimeTracker.Client.Features.TimeEntries.Services;

public class ValueTooltipData
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string? ClientName { get; init; }
    public List<EntryBreakdown> Entries { get; init; } = [];

    public class EntryBreakdown
    {
        public string TimeRange { get; init; } = string.Empty;
        public string Duration { get; init; } = string.Empty;
    }
}
