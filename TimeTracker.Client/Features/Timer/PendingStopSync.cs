using System.Text.Json;
using TimeTracker.Client.Shared;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Client.Features.Timer;

/// <summary>Persists a timer stop to local storage before attempting to save it, and retries it
/// on a later load if the save failed. Update-by-id is idempotent, so replaying it is safe.
/// Kept out of TimerPage's @code block so it can be unit-tested directly — TimerPage's
/// OnInitializedAsync runs during SSR prerender (no browser, no local storage available), so the
/// call into RetryIfAnyAsync must be guarded with OperatingSystem.IsBrowser() at the call site,
/// which makes that path unreachable when rendering the page itself in bUnit.</summary>
public class PendingStopSync(ITimeEntryService timeEntryService, ILocalStore localStore)
{
    private const string PendingStopKey = "timetracker.pendingStop";

    public record PendingStop(int EntryId, int ProjectId, DateTime Start, DateTime StopUtc, string? Note);

    /// <summary>The currently-queued stop, if any, without attempting to send it.</summary>
    public async Task<PendingStop?> GetPendingAsync()
    {
        var json = await localStore.GetItemAsync(PendingStopKey);
        return json is null ? null : JsonSerializer.Deserialize<PendingStop>(json);
    }

    /// <summary>Queues the stop locally, then attempts to save it. Returns true if it saved.
    /// If a stop for this entry is already queued (e.g. TimerPage was reloaded before the first
    /// attempt resolved), that original timestamp is preserved and retried rather than being
    /// overwritten by this call's stopUtc — the UI should never offer a second "Stop" once one is
    /// pending, but this guards the data even if it somehow does.</summary>
    public async Task<bool> StopAsync(TimeEntryResponse activeEntry, DateTime stopUtc)
    {
        var existing = await GetPendingAsync();
        if (existing is not null && existing.EntryId == activeEntry.Id)
            return await TrySendAsync(existing);

        var pending = new PendingStop(activeEntry.Id, activeEntry.Project.Id, activeEntry.Start, stopUtc, activeEntry.Note);
        await localStore.SetItemAsync(PendingStopKey, JsonSerializer.Serialize(pending));
        return await TrySendAsync(pending);
    }

    /// <summary>Retries a previously-queued stop, if any is still pending. Returns true if it
    /// saved (or there was nothing to save); false if a pending stop is still queued.</summary>
    public async Task<bool> RetryIfAnyAsync()
    {
        var pending = await GetPendingAsync();
        return pending is null || await TrySendAsync(pending);
    }

    private async Task<bool> TrySendAsync(PendingStop pending)
    {
        try
        {
            await timeEntryService.UpdateTimeEntry(pending.EntryId, new TimeEntryUpdateRequest
            {
                ProjectId = pending.ProjectId,
                Start = pending.Start,
                End = pending.StopUtc,
                Note = pending.Note
            });
            await localStore.RemoveItemAsync(PendingStopKey);
            return true;
        }
        catch (HttpRequestException)
        {
            // Still offline — stays queued in local storage for the next retry.
            return false;
        }
    }
}
