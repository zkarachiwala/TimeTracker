namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class DesktopNavigationTests : AuthenticatedDesktopPageTest
{
    [SetUp]
    public async Task StartOnTimer()
    {
        await Page.GotoAsync("/");
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
    }

    private async Task OpenDrawer()
    {
        await Page.Locator("#hamburger-btn").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Timer" }).First)
            .ToBeInViewportAsync(new() { Timeout = 5_000 });
    }

    [Test]
    public async Task DrawerOpensOnHamburgerClick()
    {
        await Page.Locator("#hamburger-btn").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Timer" }).First)
            .ToBeInViewportAsync(new() { Timeout = 5_000 });
    }

    [Test]
    public async Task DrawerNavEntriesNavigatesToEntries()
    {
        await OpenDrawer();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Entries" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/entries"));
        await Expect(Page.GetByText("Total tracked")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task DrawerNavReportsNavigatesToReports()
    {
        await OpenDrawer();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Reports" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/reports"));
        await Expect(Page.GetByText("YTD hours")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task DrawerNavProjectsNavigatesToProjects()
    {
        await OpenDrawer();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Projects" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/projects"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Projects" }).First).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task DrawerNavClientsNavigatesToClients()
    {
        await OpenDrawer();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Clients" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/clients"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Clients" }).First).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task DrawerNavTimerNavigatesBackToTimer()
    {
        await OpenDrawer();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Entries" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/entries"));
        await Expect(Page.GetByText("Total tracked")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        // Drawer stays open after client-side navigation in WASM (MainLayout state persists).
        await Page.GetByRole(AriaRole.Link, new() { Name = "Timer" }).First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/$|/\\?"));
        await Expect(Page.GetByText("Tracking now").Or(Page.GetByText("Start a timer")))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }
}
