namespace TimeTracker.Shared.Models.Project;

public record struct ProjectCreateRequest(
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate
);