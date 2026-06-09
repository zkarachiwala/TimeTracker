namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class AuthTests : PageTest
{
    private readonly List<string> _consoleErrors = [];
    private readonly List<string> _failedRequests = [];

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = TestConfig.BaseUrl,
        ViewportSize = new ViewportSize { Width = 390, Height = 844 },
        IsMobile = true,
        IgnoreHTTPSErrors = true,
    };

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

    // ── Unauthenticated redirect coverage ─────────────────────────────────────
    // Each WASM page must redirect an anonymous visitor to /login.
    // If any of these fail it means [Authorize] was removed from that page.

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
    public async Task UnauthenticatedProjectPageRedirectsToLogin()
    {
        await Page.GotoAsync("/projects/1");
        await Expect(Page).ToHaveURLAsync(new Regex("/login"));
    }

    [Test]
    public async Task UnauthenticatedClientPageRedirectsToLogin()
    {
        await Page.GotoAsync("/clients/1");
        await Expect(Page).ToHaveURLAsync(new Regex("/login"));
    }

    [Test]
    public async Task UnauthenticatedReportsRedirectsToLogin()
    {
        await Page.GotoAsync("/reports");
        await Expect(Page).ToHaveURLAsync(new Regex("/login"));
    }

    // ── Login page UI ──────────────────────────────────────────────────────────

    [Test]
    public async Task LoginPageShowsGoogleSignInButton()
    {
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15_000 });
        // MudButton with Href renders as <a>, not <button>
        await Expect(Page.GetByRole(AriaRole.Link).Filter(new() { HasText = "Google" }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task GoogleSignInLinkHasEnhanceNavDisabled()
    {
        // Regression: Blazor's enhanced navigation intercepts <a> clicks and
        // turns them into fetch requests. The OAuth challenge redirects to
        // accounts.google.com, which violates connect-src 'self' in the CSP.
        // data-enhance-nav="false" forces a full-page navigation instead.
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 15_000 });

        var link = Page.GetByRole(AriaRole.Link).Filter(new() { HasText = "Google" });
        await Expect(link).ToBeVisibleAsync();

        var attr = await link.GetAttributeAsync("data-enhance-nav");
        Assert.That(attr, Is.EqualTo("false"),
            "Google sign-in link must have data-enhance-nav=\"false\" — without it Blazor " +
            "intercepts the click as a fetch, the OAuth redirect to Google violates connect-src 'self', " +
            "and the sign-in flow is blocked by the CSP.");
    }

    // ── Access denied page ─────────────────────────────────────────────────────

    [Test]
    public async Task AccessDeniedPageIsReachable()
    {
        await Page.GotoAsync("/access-denied");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Access denied" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task AccessDeniedPageShowsBackToSignIn()
    {
        await Page.GotoAsync("/access-denied");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Back to sign in" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}
