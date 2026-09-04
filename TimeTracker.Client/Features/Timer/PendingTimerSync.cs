using System.Text.Json;
using TimeTracker.Client.Shared;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Client.Features.Timer;

/// <summary>Owns the local timer's pending-sync state across its whole lifecycle: started (possibly
/// offline, no server Id yet), stopped while still unsynced (collapsed into a single create — see
/// StopUnsyncedAsync), and stopped after already syncing (Stage A's original case, update-by-id).
/// Kept out of TimerPage's @code block so it can be unit-tested directly — TimerPage's
/// OnInitializedAsync runs during SSR prerender (no browser, no local storage available), so the
/// call into RetryIfAnyAsync must be guarded with OperatingSystem.IsBrowser() at the call site,
/// which makes that path unreachable when rendering the page itself in bUnit.</summary>
public class PendingTimerSync(ITimeEntryService timeEntryService, ILocalStore localStore)
{
    private const string PendingCreateKey = "timetracker.pendingCreate";
    private const string PendingStopKey = "timetracker.pendingStop";

    /// <summary>A timer started locally that hasn't reached the server yet — identified by a
    /// client-generated tag (ClientRequestId), not a server Id, which doesn't exist until it syncs.
    /// End is null while running, set once Stop is pressed before this ever synced (Stage B's
    /// start+stop collapse — this becomes a single Create carrying both, never a Create then an
    /// Update, which would need a local-to-server id mapping).</summary>
    public record PendingCreate(Guid ClientRequestId, int ProjectId, DateTime Start, DateTime? End, string? Note);

    /// <summary>A stop for an entry that already has a server Id — Stage A's original case.</summary>
    public record PendingStop(int EntryId, int ProjectId, DateTime Start, DateTime StopUtc, string? Note);

    // ---- Starting a timer (possibly offline) ----

    /// <summary>Starts a timer locally and attempts to sync it immediately. Returns the synced
    /// entry if the create reached the server, or null if it's still queued locally.</summary>
    public async Task<TimeEntryResponse?> StartAsync(int projectId, DateTime startUtc, string? note = null)
    {
        var pending = new PendingCreate(Guid.NewGuid(), projectId, startUtc, null, note);
        await localStore.SetItemAsync(PendingCreateKey, JsonSerializer.Serialize(pending));
        return await TrySendCreateAsync(pending);
    }

    /// <summary>The currently-queued unsynced start, if any, without attempting to send it.</summary>
    public async Task<PendingCreate?> GetPendingCreateAsync()
    {
        var json = await localStore.GetItemAsync(PendingCreateKey);
        return json is null ? null : JsonSerializer.Deserialize<PendingCreate>(json);
    }

    /// <summary>Stops a timer that never made it to the server — collapses into a single create
    /// carrying both Start and End, rather than a Create followed by an Update (which would need a
    /// local-to-server id mapping for an entry that has no server Id to update by). Returns the
    /// synced entry if it reached the server, or null if there was nothing queued, or it's still
    /// queued (offline).</summary>
    public async Task<TimeEntryResponse?> StopUnsyncedAsync(DateTime stopUtc)
    {
        var pending = await GetPendingCreateAsync();
        if (pending is null) return null;

        var updated = pending with { End = stopUtc };
        await localStore.SetItemAsync(PendingCreateKey, JsonSerializer.Serialize(updated));
        return await TrySendCreateAsync(updated);
    }

    private async Task<TimeEntryResponse?> TrySendCreateAsync(PendingCreate pending)
    {
        try
        {
            var entry = await timeEntryService.CreateTimeEntry(new TimeEntryCreateRequest
            {
                ProjectId = pending.ProjectId,
                Start = pending.Start,
                End = pending.End,
                Note = pending.Note,
                ClientRequestId = pending.ClientRequestId
            });
            await localStore.RemoveItemAsync(PendingCreateKey);
            return entry;
        }
        catch (HttpRequestException)
        {
            // Still offline — stays queued in local storage for the next retry.
            return null;
        }
    }

    // ---- Stopping an already-synced timer (Stage A, unchanged behavior) ----

    /// <summary>The currently-queued stop, if any, without attempting to send it.</summary>
    public async Task<PendingStop?> GetPendingStopAsync()
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
        var existing = await GetPendingStopAsync();
        if (existing is not null && existing.EntryId == activeEntry.Id)
            return await TrySendStopAsync(existing);

        var pending = new PendingStop(activeEntry.Id, activeEntry.Project.Id, activeEntry.Start, stopUtc, activeEntry.Note);
        await localStore.SetItemAsync(PendingStopKey, JsonSerializer.Serialize(pending));
        return await TrySendStopAsync(pending);
    }

    private async Task<bool> TrySendStopAsync(PendingStop pending)
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

    // ---- Reconciliation on page load ----

    /// <summary>Retries whichever pending mutation is queued — a create, a stop, or (in practice
    /// never both at once for the same timer session) — returning true only if nothing is left
    /// pending afterward.</summary>
    public async Task<bool> RetryIfAnyAsync()
    {
        var pendingCreate = await GetPendingCreateAsync();
        var createDone = pendingCreate is null || await TrySendCreateAsync(pendingCreate) is not null;

        var pendingStop = await GetPendingStopAsync();
        var stopDone = pendingStop is null || await TrySendStopAsync(pendingStop);

        return createDone && stopDone;
    }
}
