namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class NavigationTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task StartOnTimer()
    {
        // WaitForRequestFinishedAsync fires on 'requestfinished' (body fully downloaded), not
        // 'response' (headers only). Target the last sequential call in LoadData() — if today's
        // entries have finished, projects and active-timer must also be fully complete.
        var loadDataDone = Page.WaitForRequestFinishedAsync(new()
        {
            Predicate = r => r.Url.Contains("/api/timeentries/today"),
            Timeout = 15_000
        });
        await Page.GotoAsync("/");
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
        await loadDataDone;
        // Auth state resolves on a separate call — wait for auth-gated nav link after data is loaded
        await Expect(Page.Locator(".bottom-nav").GetByText("Clients")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task BottomNavEntriesNavigatesToEntries()
    {
        await Page.Locator(".bottom-nav a[href='entries']").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/entries"));
        await Expect(Page.GetByText("Total tracked")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task BottomNavReportsNavigatesToReports()
    {
        await Page.Locator(".bottom-nav a[href='reports']").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/reports"));
        await Expect(Page.GetByText("YTD hours")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task BottomNavProjectsNavigatesToProjects()
    {
        await Page.Locator(".bottom-nav a[href='projects']").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/projects"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Projects" }).First).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task BottomNavClientsNavigatesToClients()
    {
        await Page.Locator(".bottom-nav a[href='clients']").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/clients"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Clients" }).First).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task BottomNavTimerNavigatesBackToTimer()
    {
        await Page.Locator(".bottom-nav a[href='entries']").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/entries"));

        await Page.Locator(".bottom-nav").GetByText("Timer").ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$|/\\?"));
        await Expect(Page.GetByText("Tracking now").Or(Page.GetByText("Start a timer")))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }
}
