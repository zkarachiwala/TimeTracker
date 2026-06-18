namespace TimeTracker.Shared.Entities;

public class Client : SoftDeleteableEntity
{
    public required string Name { get; set; }
    public bool IsArchived { get; set; } = false;
    public decimal? DefaultHourlyRate { get; set; }
    public decimal? AwardRate { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public List<Project> Projects { get; set; } = [];
}
