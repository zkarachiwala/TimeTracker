namespace TimeTracker.Playwright;

[Category("Authenticated")]
public class AuthenticatedDesktopPageTest : PageTest
{
    private readonly List<string> _consoleErrors = [];
    private readonly List<string> _failedRequests = [];
    private EventHandler<IRequest>? _onRequestFailed;
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
        _failedRequests.Clear();
        _onRequestFailed = (_, req) => _failedRequests.Add($"Request failed: {req.Url}");
        _onConsoleMessage = (_, msg) =>
        {
            if (msg.Type != "error") return;
            if (msg.Text.StartsWith("Failed to load resource")) return;
            if (msg.Text.StartsWith("Failed to load module script")) return;
            _consoleErrors.Add(msg.Text);
        };
        Page.RequestFailed += _onRequestFailed;
        Page.Console += _onConsoleMessage;
    }

    [TearDown]
    public void AssertNoConsoleErrors()
    {
        if (_onRequestFailed is not null) Page.RequestFailed -= _onRequestFailed;
        if (_onConsoleMessage is not null) Page.Console -= _onConsoleMessage;
        var all = _consoleErrors.Concat(_failedRequests).ToList();
        Assert.That(all, Is.Empty,
            $"Unexpected browser errors:\n{string.Join("\n", all)}");
    }
}
