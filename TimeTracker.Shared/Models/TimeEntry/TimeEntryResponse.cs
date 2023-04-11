namespace TimeTracker.Shared.Models.TimeEntry;

public record struct TimeEntryResponse(
    int Id,
    string Project,
    DateTime Start,
    DateTime? End
);