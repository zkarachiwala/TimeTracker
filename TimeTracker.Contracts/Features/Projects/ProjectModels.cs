using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Contracts.Features.Projects;

public record ProjectResponse(
    int Id,
    string Name,
    int? ClientId,
    string? ClientName,
    decimal? HourlyRate,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate
);

public class ProjectRequest
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Please enter a name for the project.")]
    public required string Name { get; set; }
    public int? ClientId { get; set; }
    [Required(ErrorMessage = "Please enter an hourly rate.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Rate must be greater than zero.")]
    public decimal? HourlyRate { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ProjectCreateRequest
{
    public required string Name { get; set; }
    public int? ClientId { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ProjectUpdateRequest
{
    public required string Name { get; set; }
    public int? ClientId { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public record DeletedProjectResponse(int Id, string Name, string? ClientName, DateTime? DateDeleted, string? DeletedBy);
