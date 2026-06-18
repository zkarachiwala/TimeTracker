namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class TimerTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task NavigateToTimer()
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
    }

    [Test]
    public async Task StartTimerCardOrRunningCardIsVisible()
    {
        // Verifies the page actually rendered its content — catches component crashes (e.g. JsonException
        // from GetActiveTimeEntry) that leave the page blank even though the URL and FAB are present.
        var running = Page.GetByText("Tracking now");
        var idle = Page.GetByText("Start a timer");
        await Expect(running.Or(idle)).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task TodaySectionIsVisible()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Today" })).ToBeVisibleAsync();
    }

    // Write tests — skipped in CI, run locally with PLAYWRIGHT_WRITE_TESTS=true
    private static bool WriteTestsEnabled =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_WRITE_TESTS") == "true";

    [Test]
    public async Task LogFixedBlockCreatesEntry()
    {
        if (!WriteTestsEnabled) Assert.Ignore("Write tests disabled — set PLAYWRIGHT_WRITE_TESTS=true to run locally");
        if (await Page.GetByText("Tracking now").IsVisibleAsync())
            Assert.Ignore("Timer already running — skipping block test");

        await Page.GetByRole(AriaRole.Button, new() { Name = "30m" }).ClickAsync();
        await Expect(Page.GetByText("30m logged")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task StartAndStopTimerCreatesEntry()
    {
        if (!WriteTestsEnabled) Assert.Ignore("Write tests disabled — set PLAYWRIGHT_WRITE_TESTS=true to run locally");
        if (await Page.GetByText("Tracking now").IsVisibleAsync())
            Assert.Ignore("Timer already running — skipping start/stop test");

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Start timer" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Start timer" }).ClickAsync();
        await Expect(Page.GetByText("Tracking now")).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Stop & save" }).ClickAsync();
        await Expect(Page.GetByText("Timer saved")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}
