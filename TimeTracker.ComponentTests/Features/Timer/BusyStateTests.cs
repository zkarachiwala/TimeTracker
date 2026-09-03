using TimeTracker.Client.Features.Timer;

namespace TimeTracker.ComponentTests.Features.Timer;

/// <summary>Plain unit tests against BusyState directly — the disable/spinner state machine behind
/// TimerPage's Stop/Start/Sync/Log Block buttons.</summary>
public class BusyStateTests
{
    [Fact]
    public async Task RunAsync_SetsIsBusyImmediatelyOnStart()
    {
        var busy = new BusyState();
        var gate = new TaskCompletionSource();

        var run = busy.RunAsync(() => gate.Task, () => { }, TimeSpan.FromSeconds(10));

        Assert.True(busy.IsBusy);
        Assert.False(busy.ShowSpinner);

        gate.SetResult();
        await run;
    }

    [Fact]
    public async Task RunAsync_ActionCompletesBeforeDelay_NeverShowsSpinner()
    {
        var busy = new BusyState();

        await busy.RunAsync(() => Task.CompletedTask, () => { }, TimeSpan.FromSeconds(10));

        Assert.False(busy.IsBusy);
        Assert.False(busy.ShowSpinner);
    }

    [Fact]
    public async Task RunAsync_ActionOutlastsDelay_ShowsSpinnerThenClearsOnCompletion()
    {
        var busy = new BusyState();
        var gate = new TaskCompletionSource();
        var sawSpinner = new TaskCompletionSource();

        var run = busy.RunAsync(() => gate.Task, () =>
        {
            if (busy.ShowSpinner) sawSpinner.TrySetResult();
        }, TimeSpan.FromMilliseconds(20));

        await sawSpinner.Task; // waits for the delay to elapse and the spinner flag to flip
        Assert.True(busy.IsBusy);
        Assert.True(busy.ShowSpinner);

        gate.SetResult();
        await run;

        Assert.False(busy.IsBusy);
        Assert.False(busy.ShowSpinner);
    }
}
