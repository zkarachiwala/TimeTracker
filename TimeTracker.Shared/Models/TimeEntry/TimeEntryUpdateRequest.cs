namespace TimeTracker.Shared.Models.TimeEntry;

public record struct TimeEntryUpdateRequest(
    int ProjectId,
    DateTime Start,
    DateTime? End
);

