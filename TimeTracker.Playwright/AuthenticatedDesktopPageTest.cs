namespace TimeTracker.Playwright;

[Category("Authenticated")]
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

    [SetUp]
    public void MonitorConsoleErrors()
    {
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

    [TearDown]
    public void AssertNoConsoleErrors()
    {
        if (_onConsoleMessage is not null) Page.Console -= _onConsoleMessage;
        Assert.That(_consoleErrors, Is.Empty,
            $"Unexpected browser console errors:\n{string.Join("\n", _consoleErrors)}");
    }
}
