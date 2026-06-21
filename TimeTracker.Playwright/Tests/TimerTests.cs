namespace TimeTracker.Playwright.Tests;

public class TimerTests : AuthenticatedPageTest
{
    private static bool WriteTestsEnabled =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_WRITE_TESTS") == "true";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Page.RunAndWaitForRequestFinishedAsync(
            async () => await Page.GotoAsync("/"),
            new() { Predicate = r => r.Url.Contains("/api/timeentries/today"), Timeout = 15_000 }
        );
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task StartTimerCardOrRunningCardIsVisible()
    {
        // Verifies the page actually rendered its content — catches component crashes (e.g. JsonException
        // from GetActiveTimeEntry) that leave the page blank even though the URL and FAB are present.
        var running = Page.GetByText("Tracking now");
        var idle = Page.GetByText("Start a timer");
        await Expect(running.Or(idle)).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task TodaySectionIsVisible()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Today" })).ToBeVisibleAsync();
    }

    // Write tests — skipped in CI, run locally with PLAYWRIGHT_WRITE_TESTS=true

    [SkippableFact]
    public async Task LogFixedBlockCreatesEntry()
    {
        Skip.If(!WriteTestsEnabled, "Write tests disabled — set PLAYWRIGHT_WRITE_TESTS=true to run locally");
        Skip.If(await Page.GetByText("Tracking now").IsVisibleAsync(), "Timer already running — skipping block test");

        await Page.GetByRole(AriaRole.Button, new() { Name = "30m" }).ClickAsync();
        await Expect(Page.GetByText("30m logged")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [SkippableFact]
    public async Task StartAndStopTimerCreatesEntry()
    {
        Skip.If(!WriteTestsEnabled, "Write tests disabled — set PLAYWRIGHT_WRITE_TESTS=true to run locally");
        Skip.If(await Page.GetByText("Tracking now").IsVisibleAsync(), "Timer already running — skipping start/stop test");

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Start timer" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Start timer" }).ClickAsync();
        await Expect(Page.GetByText("Tracking now")).ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Stop & save" }).ClickAsync();
        await Expect(Page.GetByText("Timer saved")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}
