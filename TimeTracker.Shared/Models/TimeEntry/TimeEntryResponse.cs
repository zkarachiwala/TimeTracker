namespace TimeTracker.Shared.Models.TimeEntry;

public class TimeEntryResponse
{
    public int Id { get; set; }
    public required string Project { get; set; }
    public DateTime Start { get; set; } = DateTime.Now;
    public DateTime? End { get; set; }
}