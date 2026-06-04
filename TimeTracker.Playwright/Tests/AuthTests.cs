namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class AuthTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = TestConfig.BaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 },
        IsMobile = true,
    };

    [Test]
    public async Task UnauthenticatedRootRedirectsToLogin()
    {
        await Page.GotoAsync("/");
        await Expect(Page).ToHaveURLAsync(new Regex("/login"));
    }

    [Test]
    public async Task UnauthenticatedEntriesRedirectsToLogin()
    {
        await Page.GotoAsync("/entries");
        await Expect(Page).ToHaveURLAsync(new Regex("/login"));
    }

    [Test]
    public async Task LoginPageShowsGoogleSignInButton()
    {
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15_000 });
        // MudButton with Href renders as <a>, not <button>
        await Expect(Page.GetByRole(AriaRole.Link).Filter(new() { HasText = "Google" }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }
}
