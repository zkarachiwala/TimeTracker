using Bunit;
using MudBlazor;

namespace TimeTracker.ComponentTests.Fixtures;

// IAsyncLifetime tells xUnit to call DisposeAsync() (async teardown) rather than Dispose()
// (sync teardown). This is required because MudBlazor services (PopoverService,
// KeyInterceptorService) moved to IAsyncDisposable-only in MudBlazor 9.x, and bunit's sync
// Dispose() path throws InvalidOperationException when it encounters them. DisposeAsync()
// disposes the DI container async-safely; the subsequent sync Dispose() call that xUnit also
// makes is then a no-op (the container is already marked disposed).
public abstract class MudBlazorContext : BunitContext, IAsyncLifetime
{
    protected MudBlazorContext()
    {
        Services.AddMudServices(cfg =>
        {
            // Zero transition duration avoids fake timers in snapshot tests
            cfg.SnackbarConfiguration.ShowTransitionDuration = 0;
            cfg.SnackbarConfiguration.HideTransitionDuration = 0;
        });

        // Stub MudBlazor JS interop calls that would otherwise throw in the bUnit renderer
        JSInterop.Mode = JSRuntimeMode.Loose;

        // MudTooltip, MudMenu, and other popover-based components require MudPopoverProvider
        // to be present in the render tree before they can render.
        Render<MudPopoverProvider>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await ((IAsyncDisposable)this).DisposeAsync();
}
