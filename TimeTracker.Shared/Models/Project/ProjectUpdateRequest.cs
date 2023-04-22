namespace TimeTracker.Shared.Models.Project;

public record struct ProjectUpdateRequest(
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate
);