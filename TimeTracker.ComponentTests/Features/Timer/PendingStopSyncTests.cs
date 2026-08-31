using TimeTracker.Client.Features.Timer;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.ComponentTests.Features.Timer;

/// <summary>Plain unit tests against PendingStopSync directly — no bUnit/page render needed.
/// RetryIfAnyAsync is only ever called from TimerPage behind an OperatingSystem.IsBrowser()
/// guard (SSR prerender has no local storage), which is always false in bUnit — so testing this
/// logic through a rendered page can never reach that path. See ADR-037 / offline-resilience-plan.</summary>
public class PendingStopSyncTests
{
    private static TimeEntryResponse MakeActiveEntry(int id = 1, int projectId = 1, DateTime? start = null) =>
        new(id, new ProjectSummary(projectId, "Acme"), start ?? DateTime.UtcNow.AddMinutes(-30), null, null, null, null);

    private static (PendingStopSync Sync, FakeTimeEntryService TimeEntries, FakeLocalStore LocalStore) Make()
    {
        var timeEntries = new FakeTimeEntryService();
        var localStore = new FakeLocalStore();
        return (new PendingStopSync(timeEntries, localStore), timeEntries, localStore);
    }

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
    public async Task RetryIfAnyAsync_WithNoPendingStop_DoesNotCallUpdate()
    {
        var (sync, timeEntries, _) = Make();

        var result = await sync.RetryIfAnyAsync();

        Assert.True(result); // nothing to do counts as success — no pending state to show
        Assert.Empty(timeEntries.UpdateCalls);
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
    public async Task GetPendingAsync_WithNoPendingStop_ReturnsNull()
    {
        var (sync, _, _) = Make();

        Assert.Null(await sync.GetPendingAsync());
    }

    [Fact]
    public async Task GetPendingAsync_WithPendingStop_ReturnsIt()
    {
        var (sync, timeEntries, _) = Make();
        timeEntries.FailUpdates = true;
        var stopUtc = DateTime.UtcNow;

        await sync.StopAsync(MakeActiveEntry(id: 5, projectId: 9), stopUtc);
        var pending = await sync.GetPendingAsync();

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

        var pending = await sync.GetPendingAsync();
        Assert.NotNull(pending);
        Assert.Equal(firstStopUtc, pending.StopUtc);
    }
}
