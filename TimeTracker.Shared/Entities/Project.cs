namespace TimeTracker.Shared.Entities;

public class Project : SoftDeleteableEntity
{
    public required string  Name { get; set; }
    public List<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
}