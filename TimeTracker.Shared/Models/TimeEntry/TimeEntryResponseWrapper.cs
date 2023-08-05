namespace TimeTracker.Shared.Models.TimeEntry;

public record struct TimeEntryResponseWrapper(List<TimeEntryResponse> TimeEntries, int Count);