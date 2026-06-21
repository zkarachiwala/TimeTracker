using Bunit;
using MudBlazor;

namespace TimeTracker.ComponentTests.Fixtures;

public abstract class MudBlazorContext : TestContext
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
        RenderComponent<MudPopoverProvider>();
    }
}
