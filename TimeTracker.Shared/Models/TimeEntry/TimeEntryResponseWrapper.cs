using TimeTracker.Shared.Models.TimeEntry;

namespace TimeTracker.Shared.Models;

public record struct TimeEntryResponseWrapper(List<TimeEntryResponse> TimeEntries, int Count);