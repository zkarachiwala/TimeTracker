namespace TimeTracker.Playwright;

public class AuthenticatedPageTest : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = TestConfig.BaseUrl,
        StorageStatePath = TestConfig.AuthStatePath,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 },
        IsMobile = true,
    };
}
