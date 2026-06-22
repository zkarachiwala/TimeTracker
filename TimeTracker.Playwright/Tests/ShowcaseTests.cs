namespace TimeTracker.Playwright.Tests;

[Collection("Showcase")]
public class ShowcaseTests : PageTest
{
    private readonly List<string> _consoleErrors = [];
    private EventHandler<IConsoleMessage>? _onConsoleMessage;

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = ShowcaseFixture.ShowcaseBaseUrl,
    };

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        // Long default timeout — WASM can take 20-30s to load on a cold browser context
        Page.SetDefaultTimeout(60_000);
        _consoleErrors.Clear();
        _onConsoleMessage = (_, msg) =>
        {
            if (msg.Type != "error") return;
            // Browser logs these for any resource that 404s during WASM bootstrap; not a test failure
            if (msg.Text.StartsWith("Failed to load resource")) return;
            _consoleErrors.Add($"[{msg.Type}] {msg.Text}");
        };
        Page.Console += _onConsoleMessage;
    }

    public override async Task DisposeAsync()
    {
        if (_onConsoleMessage != null) Page.Console -= _onConsoleMessage;
        Assert.True(_consoleErrors.Count == 0,
            $"Unexpected browser console errors:\n{string.Join("\n", _consoleErrors)}");
        try { await Page.CloseAsync().WaitAsync(TimeSpan.FromSeconds(10)); } catch { }
        await base.DisposeAsync();
    }

    private async Task NavigateAsync(string path)
    {
        await Page.GotoAsync($"/TimeTracker{path}");
        // NetworkIdle is correct here: showcase uses synchronous mock services — no API
        // calls or polling after WASM loads. The only network activity is asset downloads.
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 60_000 });
    }

    [Fact]
    public async Task TimerPageLoads()
    {
        await NavigateAsync("/");
        await Expect(Page.GetByText("Start a timer").Or(Page.GetByText("Tracking now")))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task EntriesDayTabLoads()
    {
        await NavigateAsync("/entries/day");
        await Expect(Page.GetByText("Total tracked")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task EntriesCalendarTabLoads()
    {
        await NavigateAsync("/entries/calendar");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Calendar" }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task ReportsPageLoads()
    {
        await NavigateAsync("/reports");
        await Expect(Page.GetByText("YTD hours")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task ProjectsPageLoads()
    {
        await NavigateAsync("/projects");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Projects" }).First)
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task ClientsPageLoads()
    {
        await NavigateAsync("/clients");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Clients" }).First)
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task AdminUsersPageLoads()
    {
        await NavigateAsync("/admin/users");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Users" }).First)
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }
}
