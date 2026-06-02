namespace TimeTracker.Shared.Entities;

public class Project : SoftDeleteableEntity
{
    public required string Name { get; set; }
    public int? ClientId { get; set; }
    public Client? Client { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<TimeEntry>? TimeEntries { get; set; } = [];
    public List<ProjectUser> ProjectUsers { get; set; } = [];
}