namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class DesktopTimerTests : AuthenticatedDesktopPageTest
{
    [SetUp]
    public async Task NavigateToTimer()
    {
        await Page.RunAndWaitForRequestFinishedAsync(
            async () => await Page.GotoAsync("/"),
            new() { Predicate = r => r.Url.Contains("/api/timeentries/today"), Timeout = 15_000 }
        );
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
    }

    [Test]
    public async Task StartTimerCardOrRunningCardIsVisible()
    {
        var running = Page.GetByText("Tracking now");
        var idle = Page.GetByText("Start a timer");
        await Expect(running.Or(idle)).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task TodaySectionIsVisible()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Today" })).ToBeVisibleAsync();
    }
}

[TestFixture]
public class DesktopAdminNavTests : AuthenticatedDesktopPageTest
{
    [SetUp]
    public async Task NavigateToTimer()
    {
        await Page.RunAndWaitForRequestFinishedAsync(
            async () => await Page.GotoAsync("/"),
            new() { Predicate = r => r.Url.Contains("/api/timeentries/today"), Timeout = 15_000 }
        );
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
        // On desktop the nav rail is always visible — no drawer to open
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Users" }))
            .ToBeAttachedAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task UsersNavLinkIsVisibleForAdmin()
    {
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Users" }))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task UsersNavLinkNavigatesToAdminPage()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Users" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/admin/users"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Users" }).First)
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }
}

[TestFixture]
public class DesktopAdminTests : AuthenticatedDesktopPageTest
{
    private static bool WriteTestsEnabled =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_WRITE_TESTS") == "true";

    [SetUp]
    public async Task NavigateToAdminUsers()
    {
        await Page.GotoAsync("/admin/users");
        await Expect(Page.GetByPlaceholder("Email address"))
            .ToBeEnabledAsync(new() { Timeout = 30_000 });
    }

    [Test]
    public async Task UsersHeadingIsVisible()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Users" }).First)
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task AddUserFormIsVisible()
    {
        await Expect(Page.GetByPlaceholder("Email address")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Add" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task UserListShowsAtLeastOneEntry()
    {
        var userRows = Page.Locator(".mud-paper .mud-border-b");
        await Expect(userRows.First).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task CurrentUserIsMarkedAdmin()
    {
        await Expect(Page.GetByText("Admin").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task LastAdminRevokeButtonIsDisabled()
    {
        var adminCount = await Page.GetByText("Admin", new() { Exact = true }).CountAsync();
        if (adminCount > 1) Assert.Ignore("Multiple admins present — last-admin guard not testable here.");

        var revokeButton = Page.GetByRole(AriaRole.Button, new() { Name = "Revoke Admin" });
        await Expect(revokeButton).ToBeDisabledAsync();
    }
}

[TestFixture]
public class DesktopProjectDetailAdminTests : AuthenticatedDesktopPageTest
{
    [SetUp]
    public async Task NavigateToFirstProject()
    {
        await Page.GotoAsync("/projects");
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
        await Page.Locator(".mud-card").First.ClickAsync();
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 15_000 });
    }

    [Test]
    public async Task UsersHeadingIsVisibleOnProjectDetail()
    {
        await Expect(Page.GetByText("Users", new() { Exact = true }).First)
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task AddUserPickerIsVisibleOnProjectDetail()
    {
        await Expect(Page.GetByLabel("Add user")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}
