using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Shared.Models.Project;

public class ProjectRequest {
    public int Id { get; set; }
    [Required(ErrorMessage = "Please enter a name for the project.")]
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}