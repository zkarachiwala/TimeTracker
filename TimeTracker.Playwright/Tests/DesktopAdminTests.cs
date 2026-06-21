namespace TimeTracker.Playwright.Tests;

public class DesktopTimerTests : AuthenticatedDesktopPageTest
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
    public async Task StartTimerCardOrRunningCardIsVisible()
    {
        var running = Page.GetByText("Tracking now");
        var idle = Page.GetByText("Start a timer");
        await Expect(running.Or(idle)).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task TodaySectionIsVisible()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Today" })).ToBeVisibleAsync();
    }
}

/// <summary>
/// Hang-diagnostic fixture — NOT part of the normal test run.
/// Run manually to verify teardown hang behaviour after a deliberate Playwright timeout.
/// To run: temporarily remove Skip from [Fact(Skip = "...")] and target with:
/// dotnet test --filter FullyQualifiedName~HangDiagnosticTests
/// </summary>
public class HangDiagnosticTests : AuthenticatedDesktopPageTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Page.RunAndWaitForRequestFinishedAsync(
            async () => await Page.GotoAsync("/"),
            new() { Predicate = r => r.Url.Contains("/api/timeentries/today"), Timeout = 15_000 }
        );
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
        // Intentionally clicks a button that does not exist on desktop (nav rail replaced the hamburger).
        // This forces a 30s Playwright timeout to exercise teardown hang behaviour.
        await Page.Locator("#hamburger-btn").ClickAsync();
    }

    [Fact(Skip = "Hang diagnostic only — remove Skip to run manually")]
    public async Task HangDiagnostic_WillAlwaysFail() =>
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Timer" }).First)
            .ToBeInViewportAsync(new() { Timeout = 5_000 });
}

public class DesktopAdminNavTests : AuthenticatedDesktopPageTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Page.RunAndWaitForRequestFinishedAsync(
            async () => await Page.GotoAsync("/"),
            new() { Predicate = r => r.Url.Contains("/api/timeentries/today"), Timeout = 15_000 }
        );
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
        // On desktop the nav rail is always visible — no drawer to open
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Users" }))
            .ToBeAttachedAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task UsersNavLinkIsVisibleForAdmin()
    {
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Users" }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task UsersNavLinkNavigatesToAdminPage()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Users" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/admin/users"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Users" }).First)
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }
}

public class DesktopAdminTests : AuthenticatedDesktopPageTest
{
    private static bool WriteTestsEnabled =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_WRITE_TESTS") == "true";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Page.GotoAsync("/admin/users");
        await Expect(Page.GetByPlaceholder("Email address"))
            .ToBeEnabledAsync(new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task UsersHeadingIsVisible()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Users" }).First)
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task AddUserFormIsVisible()
    {
        await Expect(Page.GetByPlaceholder("Email address")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Add" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task UserListShowsAtLeastOneEntry()
    {
        var userRows = Page.Locator(".mud-paper .mud-border-b");
        await Expect(userRows.First).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CurrentUserIsMarkedAdmin()
    {
        await Expect(Page.GetByText("Admin").First).ToBeVisibleAsync();
    }

    [SkippableFact]
    public async Task LastAdminRevokeButtonIsDisabled()
    {
        var adminCount = await Page.GetByText("Admin", new() { Exact = true }).CountAsync();
        Skip.If(adminCount > 1, "Multiple admins present — last-admin guard not testable here.");

        var revokeButton = Page.GetByRole(AriaRole.Button, new() { Name = "Revoke Admin" });
        await Expect(revokeButton).ToBeDisabledAsync();
    }
}

public class DesktopProjectDetailAdminTests : AuthenticatedDesktopPageTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Page.GotoAsync("/projects");
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
        await Page.Locator(".mud-card").First.ClickAsync();
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task UsersHeadingIsVisibleOnProjectDetail()
    {
        await Expect(Page.GetByText("Users", new() { Exact = true }).First)
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task AddUserPickerIsVisibleOnProjectDetail()
    {
        await Expect(Page.GetByLabel("Add user")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}
