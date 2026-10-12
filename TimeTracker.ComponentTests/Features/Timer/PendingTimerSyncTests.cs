using TimeTracker.Client.Features.Timer;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.ComponentTests.Features.Timer;

/// <summary>Plain unit tests against PendingTimerSync directly — no bUnit/page render needed.
/// RetryIfAnyAsync is only ever called from TimerPage behind an OperatingSystem.IsBrowser()
/// guard (SSR prerender has no local storage), which is always false in bUnit — so testing this
/// logic through a rendered page can never reach that path. See ADR-037 / offline-resilience-plan.</summary>
public class PendingTimerSyncTests
{
    private static TimeEntryResponse MakeActiveEntry(int id = 1, int projectId = 1, DateTime? start = null) =>
        new(id, new ProjectSummary(projectId, "Acme"), start ?? DateTime.UtcNow.AddMinutes(-30), null, null, null, null);

    private static (PendingTimerSync Sync, FakeTimeEntryService TimeEntries, FakeLocalStore LocalStore) Make()
    {
        var timeEntries = new FakeTimeEntryService();
        var localStore = new FakeLocalStore();
        return (new PendingTimerSync(timeEntries, localStore), timeEntries, localStore);
    }

    // ---- Stopping an already-synced timer (Stage A, unchanged) ----

    [Fact]
    public async Task StopAsync_WhenSaveFails_PersistsPendingStopInLocalStorage()
    {
        var (sync, timeEntries, localStore) = Make();
        timeEntries.FailUpdates = true;

        var saved = await sync.StopAsync(MakeActiveEntry(), DateTime.UtcNow);

        Assert.False(saved);
        Assert.True(localStore.Contains("timetracker.pendingStop"));
    }

    [Fact]
    public async Task StopAsync_WhenSaveSucceeds_ClearsPendingStopFromLocalStorage()
    {
        var (sync, _, localStore) = Make();

        var saved = await sync.StopAsync(MakeActiveEntry(), DateTime.UtcNow);

        Assert.True(saved);
        Assert.False(localStore.Contains("timetracker.pendingStop"));
    }

    [Fact]
    public async Task RetryIfAnyAsync_WithPendingStopFromPriorFailure_SendsOriginalStopTime()
    {
        var (sync, timeEntries, localStore) = Make();
        var active = MakeActiveEntry(id: 42, projectId: 7);
        timeEntries.FailUpdates = true;
        await sync.StopAsync(active, active.Start.AddHours(1));
        var stoppedAt = Assert.Single(timeEntries.UpdateCalls).Request.End;
        timeEntries.UpdateCalls.Clear();

        // Backend comes back; a later page load retries the queued stop.
        timeEntries.FailUpdates = false;
        var synced = await sync.RetryIfAnyAsync();

        Assert.True(synced);
        var retryCall = Assert.Single(timeEntries.UpdateCalls);
        Assert.Equal(42, retryCall.Id);
        Assert.Equal(stoppedAt, retryCall.Request.End);
        Assert.False(localStore.Contains("timetracker.pendingStop"));
    }

    [Fact]
    public async Task RetryIfAnyAsync_WithNothingPending_ReturnsTrueAndCallsNothing()
    {
        var (sync, timeEntries, _) = Make();

        var result = await sync.RetryIfAnyAsync();

        Assert.True(result); // nothing to do counts as success — no pending state to show
        Assert.Empty(timeEntries.UpdateCalls);
        Assert.Empty(timeEntries.CreateCalls);
    }

    [Fact]
    public async Task RetryIfAnyAsync_WhenStillOffline_LeavesPendingStopQueued()
    {
        var (sync, timeEntries, localStore) = Make();
        timeEntries.FailUpdates = true;
        await sync.StopAsync(MakeActiveEntry(), DateTime.UtcNow);

        var result = await sync.RetryIfAnyAsync(); // still offline

        Assert.False(result);
        Assert.True(localStore.Contains("timetracker.pendingStop"));
    }

    [Fact]
    public async Task GetPendingStopAsync_WithNoPendingStop_ReturnsNull()
    {
        var (sync, _, _) = Make();

        Assert.Null(await sync.GetPendingStopAsync());
    }

    [Fact]
    public async Task GetPendingStopAsync_WithPendingStop_ReturnsIt()
    {
        var (sync, timeEntries, _) = Make();
        timeEntries.FailUpdates = true;
        var stopUtc = DateTime.UtcNow;

        await sync.StopAsync(MakeActiveEntry(id: 5, projectId: 9), stopUtc);
        var pending = await sync.GetPendingStopAsync();

        Assert.NotNull(pending);
        Assert.Equal(5, pending.EntryId);
        Assert.Equal(9, pending.ProjectId);
        Assert.Equal(stopUtc, pending.StopUtc);
    }

    [Fact]
    public async Task StopAsync_CalledTwiceForSameEntry_PreservesOriginalStopTime()
    {
        // Regression: a second Stop for the same entry (e.g. a stale click reaching the handler
        // before the UI updates) must not clobber the already-queued stop with a later timestamp.
        var (sync, timeEntries, _) = Make();
        var active = MakeActiveEntry(id: 42, projectId: 7);
        timeEntries.FailUpdates = true;

        var firstStopUtc = active.Start.AddSeconds(10);
        await sync.StopAsync(active, firstStopUtc);

        var secondStopUtc = active.Start.AddSeconds(25);
        await sync.StopAsync(active, secondStopUtc);

        var pending = await sync.GetPendingStopAsync();
        Assert.NotNull(pending);
        Assert.Equal(firstStopUtc, pending.StopUtc);
    }

    // ---- Starting a timer, possibly offline (Stage B) ----

    [Fact]
    public async Task StartAsync_WhenSaveFails_PersistsPendingCreateInLocalStorage()
    {
        var (sync, timeEntries, localStore) = Make();
        timeEntries.FailCreates = true;

        var result = await sync.StartAsync(projectId: 3, startUtc: DateTime.UtcNow);

        Assert.Null(result);
        Assert.True(localStore.Contains("timetracker.pendingCreate"));
    }

    [Fact]
    public async Task StartAsync_WhenSaveSucceeds_ClearsPendingCreateAndReturnsSyncedEntry()
    {
        var (sync, _, localStore) = Make();

        var result = await sync.StartAsync(projectId: 3, startUtc: DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.False(localStore.Contains("timetracker.pendingCreate"));
    }

    [Fact]
    public async Task StartAsync_SendsWithNoEnd_SinceTheTimerIsStillRunning()
    {
        var (sync, timeEntries, _) = Make();

        await sync.StartAsync(projectId: 3, startUtc: DateTime.UtcNow);

        var call = Assert.Single(timeEntries.CreateCalls);
        Assert.Null(call.End);
    }

    [Fact]
    public async Task GetPendingCreateAsync_WithNoPendingCreate_ReturnsNull()
    {
        var (sync, _, _) = Make();

        Assert.Null(await sync.GetPendingCreateAsync());
    }

    [Fact]
    public async Task StopUnsyncedAsync_WithNoPendingCreate_ReturnsNull()
    {
        var (sync, _, _) = Make();

        Assert.Null(await sync.StopUnsyncedAsync(DateTime.UtcNow));
    }

    [Fact]
    public async Task StopUnsyncedAsync_CollapsesStartAndStopIntoASingleCreateCall()
    {
        // Regression coverage for the core Stage B guarantee: a timer started and stopped while
        // never online must sync as one Create carrying both Start and End — never a Create
        // followed by an Update, which would need a local-to-server id mapping that doesn't exist
        // for an entry the server has never seen.
        var (sync, timeEntries, _) = Make();
        timeEntries.FailCreates = true;
        var startUtc = DateTime.UtcNow.AddMinutes(-10);
        await sync.StartAsync(projectId: 3, startUtc: startUtc);

        timeEntries.FailCreates = false;
        var stopUtc = DateTime.UtcNow;
        var result = await sync.StopUnsyncedAsync(stopUtc);

        Assert.NotNull(result);
        // Two attempts are recorded (the first failed while offline, the second succeeded) — what
        // matters is the one that actually landed carries both Start and End together.
        var lastCall = timeEntries.CreateCalls.Last();
        Assert.Equal(startUtc, lastCall.Start);
        Assert.Equal(stopUtc, lastCall.End);
        Assert.Empty(timeEntries.UpdateCalls); // never an Update — no server id ever existed to update by
    }

    [Fact]
    public async Task StartAsync_CalledTwiceWhileOffline_SendsSameClientRequestIdBothTimes()
    {
        // The idempotency tag must survive a retry — RetryIfAnyAsync re-sends the exact same
        // pending create, not a fresh one with a new tag, or the server-side dedup would never fire.
        var (sync, timeEntries, _) = Make();
        timeEntries.FailCreates = true;
        await sync.StartAsync(projectId: 3, startUtc: DateTime.UtcNow);
        var firstTag = Assert.Single(timeEntries.CreateCalls).ClientRequestId;

        timeEntries.FailCreates = false;
        await sync.RetryIfAnyAsync();

        Assert.Equal(2, timeEntries.CreateCalls.Count);
        Assert.Equal(firstTag, timeEntries.CreateCalls[1].ClientRequestId);
        Assert.NotNull(firstTag);
    }

    [Fact]
    public async Task RetryIfAnyAsync_WithPendingCreateFromPriorFailure_SyncsItAndClearsLocalStorage()
    {
        var (sync, timeEntries, localStore) = Make();
        timeEntries.FailCreates = true;
        await sync.StartAsync(projectId: 3, startUtc: DateTime.UtcNow);

        timeEntries.FailCreates = false;
        var result = await sync.RetryIfAnyAsync();

        Assert.True(result);
        Assert.False(localStore.Contains("timetracker.pendingCreate"));
    }

    [Fact]
    public async Task RetryIfAnyAsync_WhenCreateStillOffline_LeavesPendingCreateQueued()
    {
        var (sync, timeEntries, localStore) = Make();
        timeEntries.FailCreates = true;
        await sync.StartAsync(projectId: 3, startUtc: DateTime.UtcNow);

        var result = await sync.RetryIfAnyAsync(); // still offline

        Assert.False(result);
        Assert.True(localStore.Contains("timetracker.pendingCreate"));
    }
}
