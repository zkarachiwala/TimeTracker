using TimeTracker.Shared.Models.Project;

namespace TimeTracker.Shared.Models.TimeEntry;

public record struct TimeEntryResponse(
    int Id,
    ProjectResponse Project,
    DateTime Start,
    DateTime? End
);