using TimeTracker.Client.Features.Timer.Pages;
using TimeTracker.Client.Shared;
using TimeTracker.ComponentTests.Fixtures;
using TimeTracker.Contracts.Features.Projects;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.ComponentTests.Features.Timer;

public class TimerPageTests : MudBlazorContext
{
    private FakeTimeEntryService _timeEntries = null!;
    private FakeLocalStore _localStore = null!;

    protected override void ConfigureServices()
    {
        _timeEntries = new FakeTimeEntryService();
        _localStore = new FakeLocalStore();
        Services.AddScoped<ITimeEntryService>(_ => _timeEntries);
        Services.AddScoped<IProjectService>(_ => new FakeProjectService([MakeProject()]));
        Services.AddScoped<ILocalStore>(_ => _localStore);
    }

    private static TimeEntryResponse MakeActiveEntry(int id = 1, int projectId = 1, DateTime? start = null) =>
        new(id, new ProjectSummary(projectId, "Acme"), start ?? DateTime.UtcNow.AddMinutes(-30), null, null, null, null);

    private static ProjectResponse MakeProject(int id = 1, string name = "Acme") =>
        new(id, name, null, null, null, null, null, null);

    [Fact]
    public async Task StopTimer_WhenSaveFails_PersistsPendingStopInLocalStorage()
    {
        _timeEntries.ActiveEntry = MakeActiveEntry();
        _timeEntries.FailUpdates = true;

        var cut = Render<TimerPage>();
        await cut.InvokeAsync(() => cut.Find("button:contains('Stop')").Click());

        Assert.True(_localStore.Contains("timetracker.pendingStop"));
    }

    [Fact]
    public async Task StopTimer_WhenSaveSucceeds_ClearsPendingStopFromLocalStorage()
    {
        _timeEntries.ActiveEntry = MakeActiveEntry();

        var cut = Render<TimerPage>();
        await cut.InvokeAsync(() => cut.Find("button:contains('Stop')").Click());

        Assert.False(_localStore.Contains("timetracker.pendingStop"));
    }

    [Fact]
    public async Task StopTimer_WhenSaveFails_SendsOriginalStopTimeOnRetry()
    {
        var active = MakeActiveEntry(id: 42, projectId: 7);
        _timeEntries.ActiveEntry = active;
        _timeEntries.FailUpdates = true;

        var cut = Render<TimerPage>();
        await cut.InvokeAsync(() => cut.Find("button:contains('Stop')").Click());

        var firstAttempt = Assert.Single(_timeEntries.UpdateCalls);
        var stoppedAt = firstAttempt.Request.End;
        Assert.NotNull(stoppedAt);

        // Simulate the backend coming back and the page reloading: retry from the persisted
        // pending stop must resend the *original* stop timestamp, not a new one.
        _timeEntries.FailUpdates = false;
        _timeEntries.UpdateCalls.Clear();
        await Render<TimerPage>().InvokeAsync(() => { });

        var retryCall = Assert.Single(_timeEntries.UpdateCalls);
        Assert.Equal(42, retryCall.Id);
        Assert.Equal(stoppedAt, retryCall.Request.End);
        Assert.False(_localStore.Contains("timetracker.pendingStop"));
    }

    [Fact]
    public async Task OnLoad_WithNoPendingStop_DoesNotCallUpdate()
    {
        _timeEntries.ActiveEntry = MakeActiveEntry();

        var cut = Render<TimerPage>();
        await cut.InvokeAsync(() => { });

        Assert.Empty(_timeEntries.UpdateCalls);
    }
}
