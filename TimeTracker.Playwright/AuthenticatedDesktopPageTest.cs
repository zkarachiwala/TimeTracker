namespace TimeTracker.Playwright;

[Collection("App")]
public class AuthenticatedDesktopPageTest : PageTest
{
    private readonly List<string> _consoleErrors = [];
    private EventHandler<IConsoleMessage>? _onConsoleMessage;

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = TestConfig.BaseUrl,
        StorageStatePath = TestConfig.AuthStatePath,
        ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
        IsMobile = false,
        IgnoreHTTPSErrors = true,
    };

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Page.SetDefaultTimeout(30_000);
        _consoleErrors.Clear();
        _onConsoleMessage = (_, msg) =>
        {
            if (msg.Type != "error") return;
            if (msg.Text.StartsWith("Failed to load resource")) return;
            if (msg.Text.StartsWith("Failed to load module script")) return;
            _consoleErrors.Add(msg.Text);
        };
        Page.Console += _onConsoleMessage;
    }

    public override async Task DisposeAsync()
    {
        if (_onConsoleMessage is not null) Page.Console -= _onConsoleMessage;
        Assert.True(_consoleErrors.Count == 0,
            $"Unexpected browser console errors:\n{string.Join("\n", _consoleErrors)}");
        try
        {
            if (Page is not null)
                await Page.CloseAsync().WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
        catch { }
        await base.DisposeAsync();
    }
}
