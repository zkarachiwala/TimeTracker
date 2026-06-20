namespace TimeTracker.Playwright;

[Category("Authenticated")]
public class AuthenticatedPageTest : PageTest
{
    private readonly List<string> _consoleErrors = [];
    private EventHandler<IConsoleMessage>? _onConsoleMessage;

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = TestConfig.BaseUrl,
        StorageStatePath = TestConfig.AuthStatePath,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 },
        IsMobile = true,
        IgnoreHTTPSErrors = true,
    };

    [SetUp]
    public void MonitorConsoleErrors()
    {
        Page.SetDefaultTimeout(120_000);
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

    [TearDown]
    public async Task PageTearDownAsync()
    {
        try
        {
            if (Page is not null)
                await Page.CloseAsync().ConfigureAwait(false);
        }
        catch { }
    }
}
