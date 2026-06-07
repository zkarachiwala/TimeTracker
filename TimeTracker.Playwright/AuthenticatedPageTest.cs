namespace TimeTracker.Playwright;

public class AuthenticatedPageTest : PageTest
{
    private readonly List<string> _consoleErrors = [];

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
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error") _consoleErrors.Add(msg.Text);
        };
    }

    [TearDown]
    public void AssertNoConsoleErrors()
    {
        Assert.That(_consoleErrors, Is.Empty,
            $"Unexpected browser console errors:\n{string.Join("\n", _consoleErrors)}");
    }
}
