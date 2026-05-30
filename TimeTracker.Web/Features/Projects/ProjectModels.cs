using System.ComponentModel.DataAnnotations;
using Mapster;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Features.Projects;

public record ProjectResponse(
    int Id,
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate
);

public class ProjectRequest
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Please enter a name for the project.")]
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ProjectCreateRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ProjectUpdateRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ProjectMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Project, ProjectResponse>()
            .Map(dest => dest.Description, src => src.ProjectDetails != null ? src.ProjectDetails.Description : null)
            .Map(dest => dest.StartDate, src => src.ProjectDetails != null ? src.ProjectDetails.StartDate : null)
            .Map(dest => dest.EndDate, src => src.ProjectDetails != null ? src.ProjectDetails.EndDate : null);
    }
}
