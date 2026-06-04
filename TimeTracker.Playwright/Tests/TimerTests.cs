namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class TimerTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task NavigateToTimer()
    {
        await Page.GotoAsync("/");
        // Blazor SignalR connection can take a moment on cold start
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30_000 });
    }

    [Test]
    public async Task TimerPageLoads()
    {
        await Expect(Page).ToHaveTitleAsync(new Regex("Timer"));
    }

    [Test]
    public async Task StartTimerCardOrRunningCardIsVisible()
    {
        var running = Page.GetByText("Tracking now");
        var idle = Page.GetByText("Start a timer");

        var hasRunning = await running.IsVisibleAsync();
        var hasIdle = await idle.IsVisibleAsync();

        Assert.That(hasRunning || hasIdle, Is.True,
            "Expected either a running timer card or the start-timer card to be visible");
    }

    [Test]
    public async Task TodaySectionIsVisible()
    {
        await Expect(Page.GetByText("Today")).ToBeVisibleAsync();
    }

    [Test]
    public async Task FabButtonIsVisible()
    {
        await Expect(Page.Locator(".tt-fab button")).ToBeVisibleAsync();
    }

    [Test]
    public async Task LogFixedBlockCreatesEntry()
    {
        if (await Page.GetByText("Tracking now").IsVisibleAsync())
            Assert.Ignore("Timer already running — skipping block test");

        await Page.GetByRole(AriaRole.Button, new() { Name = "30m" }).ClickAsync();

        await Expect(Page.GetByText("30m logged")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task StartAndStopTimerCreatesEntry()
    {
        if (await Page.GetByText("Tracking now").IsVisibleAsync())
            Assert.Ignore("Timer already running — skipping start/stop test");

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Start timer" })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Start timer" }).ClickAsync();

        await Expect(Page.GetByText("Tracking now")).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // "Stop & save" button on the running card
        await Page.GetByRole(AriaRole.Button, new() { Name = "Stop & save" }).ClickAsync();

        await Expect(Page.GetByText("Timer saved")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}
