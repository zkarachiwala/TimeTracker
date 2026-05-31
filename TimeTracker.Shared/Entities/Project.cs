namespace TimeTracker.Shared.Entities;

public class Project : SoftDeleteableEntity
{
    public required string Name { get; set; }
    public int? ClientId { get; set; }
    public Client? Client { get; set; }
    public decimal? HourlyRate { get; set; }
    public List<TimeEntry>? TimeEntries { get; set; } = [];
    public ProjectDetails? ProjectDetails { get; set; }
    public List<ProjectUser> ProjectUsers { get; set; } = [];
}