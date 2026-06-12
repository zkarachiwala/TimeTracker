namespace TimeTracker.Playwright;

[Category("Authenticated")]
public class AuthenticatedPageTest : PageTest
{
    private readonly List<string> _consoleErrors = [];
    private readonly List<string> _failedRequests = [];

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
        _consoleErrors.Clear();
        _failedRequests.Clear();
        Page.RequestFailed += (_, req) => _failedRequests.Add($"Request failed: {req.Url}");
        Page.Console += (_, msg) =>
        {
            if (msg.Type != "error") return;
            if (msg.Text.StartsWith("Failed to load resource")) return;
            if (msg.Text.StartsWith("Failed to load module script")) return;
            _consoleErrors.Add(msg.Text);
        };
    }

    [TearDown]
    public void AssertNoConsoleErrors()
    {
        var all = _consoleErrors.Concat(_failedRequests).ToList();
        Assert.That(all, Is.Empty,
            $"Unexpected browser errors:\n{string.Join("\n", all)}");
    }
}
