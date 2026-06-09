namespace TimeTracker.Playwright;

[Category("Authenticated")]
public class AuthenticatedDesktopPageTest : PageTest
{
    private readonly List<string> _consoleErrors = [];
    private readonly List<string> _failedRequests = [];

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = TestConfig.BaseUrl,
        StorageStatePath = TestConfig.AuthStatePath,
        ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
        IsMobile = false,
        IgnoreHTTPSErrors = true,
    };

    [OneTimeSetUp]
    public void RequireAuthState()
    {
        if (!File.Exists(TestConfig.AuthStatePath))
            Assert.Ignore("Auth state not found — run CaptureAuthState locally first (see AuthSetup.cs)");
    }

    [SetUp]
    public void MonitorConsoleErrors()
    {
        Page.RequestFailed += (_, req) =>
        {
            if (!req.Url.EndsWith(".pdb"))
                _failedRequests.Add($"Request failed: {req.Url}");
        };
        Page.Console += (_, msg) =>
        {
            if (msg.Type != "error") return;
            if (msg.Text.StartsWith("Failed to load resource")) return;
            if (msg.Text.Contains(".pdb")) return;
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
