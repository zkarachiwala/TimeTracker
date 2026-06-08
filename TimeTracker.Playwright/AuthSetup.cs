namespace TimeTracker.Playwright;

/// <summary>
/// Run this once before running the full suite locally to capture auth state.
/// Requires the app to be running in Development mode with a seeded user.
/// Usage: PLAYWRIGHT_BASE_URL=https://localhost:7006 dotnet test --filter "FullyQualifiedName~CaptureAuthState"
/// </summary>
[TestFixture]
public class AuthSetup : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = TestConfig.BaseUrl,
        IgnoreHTTPSErrors = true,
    };

    [Test]
    public async Task CaptureAuthState()
    {
        // Use the DEV-only login endpoint — no headed browser or OAuth flow needed
        await Page.GotoAsync("/api/dev/login");
        await Expect(Page.GetByText("Signed in as")).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Navigate to the app and wait for WASM to fully load
        await Page.GotoAsync("/");
        await Page.WaitForSelectorAsync(".tt-fab button", new() { Timeout = 60_000 });

        var authDir = Path.GetDirectoryName(TestConfig.AuthStatePath)!;
        Directory.CreateDirectory(authDir);
        await Page.Context.StorageStateAsync(new() { Path = TestConfig.AuthStatePath });

        Console.WriteLine($"Auth state saved to: {TestConfig.AuthStatePath}");
    }
}
