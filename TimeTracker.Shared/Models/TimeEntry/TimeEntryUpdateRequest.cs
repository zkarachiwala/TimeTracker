namespace TimeTracker.Shared.Models.TimeEntry;

public record struct TimeEntryUpdateRequest(
    string Project,
    DateTime Start,
    DateTime? End
);

