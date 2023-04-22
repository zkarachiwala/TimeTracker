namespace TimeTracker.Shared.Models.TimeEntry;

public record struct TimeEntryCreateRequest(
    int ProjectId,
    DateTime Start,
    DateTime? End
);
