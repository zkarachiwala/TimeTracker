namespace TimeTracker.Shared.Models.Project;

public record struct ProjectResponse(
    int Id,
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate
);