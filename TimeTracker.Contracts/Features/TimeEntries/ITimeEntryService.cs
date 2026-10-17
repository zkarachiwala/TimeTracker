namespace TimeTracker.Contracts.Features.TimeEntries;

public interface ITimeEntryService
{
    Task<TimeEntryResponse?> GetTimeEntryById(int id, CancellationToken ct = default);

    /// <summary>Idempotent when request.ClientRequestId is set — a repeated call with the same tag
    /// returns the existing row instead of inserting a duplicate (Stage B offline sync).</summary>
    Task<TimeEntryResponse> CreateTimeEntry(TimeEntryCreateRequest request, CancellationToken ct = default);
    Task UpdateTimeEntry(int id, TimeEntryUpdateRequest request, CancellationToken ct = default);
    Task DeleteTimeEntry(int id, CancellationToken ct = default);
    /// <param name="timeZoneId">Caller's IANA time zone — "year" means the caller's local calendar year.</param>
    Task<List<TimeEntryResponse>> GetAllTimeEntriesByYear(int year, string timeZoneId, CancellationToken ct = default);
    Task<TimeEntryResponse?> GetActiveTimeEntry(CancellationToken ct = default);
    /// <param name="timeZoneId">Caller's IANA time zone — "today" means the caller's local day.</param>
    Task<List<TimeEntryResponse>> GetTodaysTimeEntries(string timeZoneId, CancellationToken ct = default);
    Task<List<TimeEntryResponse>> GetAllTimeEntriesByProject(int projectId, CancellationToken ct = default);
    Task<List<DeletedTimeEntryResponse>> GetDeletedTimeEntries(CancellationToken ct = default);
    Task RestoreTimeEntry(int id, CancellationToken ct = default);
}
