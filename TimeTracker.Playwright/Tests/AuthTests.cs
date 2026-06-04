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
        // Button text is "Sign in with Google" rendered by ASP.NET Identity external providers
        await Expect(Page.GetByRole(AriaRole.Button).Filter(new() { HasText = "Sign in with Google" }))
            .ToBeVisibleAsync();
    }
}
