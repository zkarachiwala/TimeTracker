namespace TimeTracker.Playwright.Tests;

public class DesktopNavigationTests : AuthenticatedDesktopPageTest
{
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
    public async Task RailNavEntriesNavigatesToEntries()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Entries" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/entries"));
        await Expect(Page.GetByText("Total tracked")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task RailNavReportsNavigatesToReports()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Reports" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/reports"));
        await Expect(Page.GetByText("YTD hours")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task RailNavProjectsNavigatesToProjects()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Projects" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/projects"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Projects" }).First).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task RailNavClientsNavigatesToClients()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Clients" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/clients"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Clients" }).First).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task AvatarDropdownOpensWithSignOut()
    {
        await Page.Locator("[title='Account']").ClickAsync();
        await Expect(Page.GetByText("Sign out"))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });
    }

    [Fact]
    public async Task RailNavTimerNavigatesBackToTimer()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Entries" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/entries"));
        await Expect(Page.GetByText("Total tracked")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await Page.GetByRole(AriaRole.Link, new() { Name = "Timer" }).First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$|/\\?"));
        await Expect(Page.GetByText("Tracking now").Or(Page.GetByText("Start a timer")))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }
}
