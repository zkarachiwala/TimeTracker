namespace TimeTracker.Client.Features.Timer;

/// <summary>Drives the "disable immediately → spinner only if still unresolved after ~1s" feedback
/// pattern for a single save action, per NN/g's response-time thresholds (0.1–1s needs no feedback
/// beyond disabling the control; only past ~1s does a spinner demonstrably help perceived wait —
/// see docs/plans/stage-b-start-timer-offline-plan.md). One instance per independently-clickable
/// action — TimerPage keeps separate instances for Stop, Start, Sync, and each Log Block chip so
/// clicking one doesn't disable or spin the others.</summary>
public class BusyState
{
    public bool IsBusy { get; private set; }
    public bool ShowSpinner { get; private set; }

    /// <summary>Runs action, calling onChanged whenever IsBusy/ShowSpinner change so the caller can
    /// re-render. spinnerDelay defaults to 1 second; overridable for tests.</summary>
    public async Task RunAsync(Func<Task> action, Action onChanged, TimeSpan? spinnerDelay = null)
    {
        IsBusy = true;
        ShowSpinner = false;
        onChanged();

        using var cts = new CancellationTokenSource();
        var spinnerTask = ShowSpinnerAfterDelay(spinnerDelay ?? TimeSpan.FromSeconds(1), onChanged, cts.Token);

        try
        {
            await action();
        }
        finally
        {
            cts.Cancel();
            await spinnerTask;
            IsBusy = false;
            ShowSpinner = false;
            onChanged();
        }
    }

    private async Task ShowSpinnerAfterDelay(TimeSpan delay, Action onChanged, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);
            ShowSpinner = true;
            onChanged();
        }
        catch (TaskCanceledException)
        {
            // The action resolved before the delay elapsed — no spinner needed.
        }
    }
}
