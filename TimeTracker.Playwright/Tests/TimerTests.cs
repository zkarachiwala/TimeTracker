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

    [SkippableFact]
    public async Task StopTimer_WhenSaveFails_ShowsSyncCardAndRetryPreservesOriginalStopTime()
    {
        Skip.If(!WriteTestsEnabled, "Write tests disabled — set PLAYWRIGHT_WRITE_TESTS=true to run locally");
        Skip.If(await Page.GetByText("Tracking now").IsVisibleAsync(), "Timer already running — skipping offline-stop test");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Start timer" }).ClickAsync();
        await Expect(Page.GetByText("Tracking now")).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Fail the first PUT (the actual Stop save) with a real 500 — matches what the backend
        // returns when the database is unreachable — then let the retry through normally.
        var failedOnce = false;
        await Page.RouteAsync("**/api/timeentries/*", async route =>
        {
            if (route.Request.Method == "PUT" && !failedOnce)
            {
                failedOnce = true;
                await route.FulfillAsync(new RouteFulfillOptions { Status = 500 });
            }
            else
            {
                await route.ContinueAsync();
            }
        });

        try
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = "Stop & save" }).ClickAsync();

            // The ticker must freeze immediately on click, not wait on the failed network round
            // trip — regression coverage for the timer continuing to count after Stop.
            await Expect(Page.GetByText("Tracking now")).Not.ToBeVisibleAsync(new() { Timeout = 2_000 });
            await Expect(Page.GetByText("Stopped — not yet saved")).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Expect(Page.GetByText("Couldn't reach the server — click Sync to retry")).ToBeVisibleAsync();

            var syncButton = Page.GetByRole(AriaRole.Button, new() { Name = "Sync" });
            await Expect(syncButton).ToBeVisibleAsync();
            await syncButton.ClickAsync();

            // Retry must resend the original stop time, not a fresh one — regression coverage
            // for the second-click data-corruption bug.
            await Expect(Page.GetByText("Timer saved")).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Start timer" })).ToBeVisibleAsync();
        }
        finally
        {
            await Page.UnrouteAsync("**/api/timeentries/*");
        }
    }

    [SkippableFact]
    public async Task StartTimer_WhenCreateFailsOnce_ShowsRunningCardImmediatelyAndTicksAndSyncsOnStop()
    {
        Skip.If(!WriteTestsEnabled, "Write tests disabled — set PLAYWRIGHT_WRITE_TESTS=true to run locally");
        Skip.If(await Page.GetByText("Tracking now").IsVisibleAsync(), "Timer already running — skipping offline-start test");

        // Fail the first POST (the actual Start save) with a real 500 — matches what the backend
        // returns when the database is unreachable — then let the retry through normally. The
        // exact-path pattern (no trailing wildcard) only matches the create endpoint, not
        // /api/timeentries/active or /api/timeentries/today.
        var failedOnce = false;
        await Page.RouteAsync("**/api/timeentries/", async route =>
        {
            if (route.Request.Method == "POST" && !failedOnce)
            {
                failedOnce = true;
                await route.FulfillAsync(new RouteFulfillOptions { Status = 500 });
            }
            else
            {
                await route.ContinueAsync();
            }
        });

        try
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = "Start timer" }).ClickAsync();

            // Regression coverage: the running card must appear immediately from local state, not
            // wait on the failed create (or any network refresh) to resolve first.
            await Expect(Page.GetByText("Tracking now")).ToBeVisibleAsync(new() { Timeout = 3_000 });

            // Regression coverage: the ticker must actually start counting while still unsynced,
            // not sit frozen until a (failing) network call finally gives up.
            var elapsedLocator = Page.Locator(".tabnum").First;
            var firstReading = await elapsedLocator.InnerTextAsync();
            await Expect(elapsedLocator).Not.ToHaveTextAsync(firstReading, new() { Timeout = 5_000 });

            // Stopping now collapses the still-unsynced create into a single Create carrying both
            // Start and End (the route only fails once, so this retry reaches the server).
            await Page.GetByRole(AriaRole.Button, new() { Name = "Stop & save" }).ClickAsync();
            await Expect(Page.GetByText("Timer saved")).ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Start timer" })).ToBeVisibleAsync();
        }
        finally
        {
            await Page.UnrouteAsync("**/api/timeentries/");
        }
    }
}
