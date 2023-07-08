namespace TimeTracker.Shared.Entities;

public class TimeEntry : BaseEntity
{
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateTime Start { get; set; } = DateTime.Now;
    public DateTime? End { get; set; }    
    public required string UserId { get; set; }
}

