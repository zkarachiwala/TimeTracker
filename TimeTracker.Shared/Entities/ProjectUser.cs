using System.Formats.Asn1;

namespace TimeTracker.Shared.Entities;

public class ProjectUser
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}