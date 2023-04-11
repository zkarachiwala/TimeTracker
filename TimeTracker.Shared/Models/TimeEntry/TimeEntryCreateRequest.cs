namespace TimeTracker.Shared.Models.TimeEntry;

public record struct TimeEntryCreateRequest(
    string Project,
    DateTime Start,
    DateTime? End
);
