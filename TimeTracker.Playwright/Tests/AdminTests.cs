namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class AdminTests : AuthenticatedPageTest
{
    private static bool WriteTestsEnabled =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_WRITE_TESTS") == "true";

    [SetUp]
    public async Task NavigateToAdminUsers()
    {
        await Page.GotoAsync("/admin/users");
        // Wait for WASM to hydrate — email field becomes enabled when interactive
        await Expect(Page.GetByPlaceholder("Email address"))
            .ToBeEnabledAsync(new() { Timeout = 30_000 });
    }

    // ── Page load ─────────────────────────────────────────────────────────────

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
        // The logged-in admin must appear in the list
        var userRows = Page.Locator(".mud-paper .mud-border-b");
        await Expect(userRows.First).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Test]
    public async Task CurrentUserIsMarkedAdmin()
    {
        // The test account bootstrapped via Authentication:AdminEmail must show as Admin
        await Expect(Page.GetByText("Admin").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task LastAdminRevokeButtonIsDisabled()
    {
        // If only one admin exists the Revoke Admin button must be disabled to prevent lockout
        var adminCount = await Page.GetByText("Admin", new() { Exact = true }).CountAsync();
        if (adminCount > 1) Assert.Ignore("Multiple admins present — last-admin guard not testable here.");

        var revokeButton = Page.GetByRole(AriaRole.Button, new() { Name = "Revoke Admin" });
        await Expect(revokeButton).ToBeDisabledAsync();
    }

    // ── Write tests ──────────────────────────────────────────────────────────

    [Test]
    public async Task AddUser_AppearsInUserList()
    {
        if (!WriteTestsEnabled) Assert.Ignore("Write tests disabled — set PLAYWRIGHT_WRITE_TESTS=true to run locally");

        var email = $"playwright-{Guid.NewGuid():N}@example.com";

        await Page.GetByPlaceholder("Email address").FillAsync(email);
        await Page.GetByPlaceholder("Email address").PressAsync("Tab");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Add" })).ToBeEnabledAsync(new() { Timeout = 5_000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();

        // Wait for the new row to appear in the user list
        await Expect(Page.GetByText(email, new() { Exact = true })).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}

[TestFixture]
public class AdminNavTests : AuthenticatedPageTest
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
        // Auth state resolves on a separate call — wait for auth-gated nav link after data is loaded
        await Expect(Page.Locator(".bottom-nav").GetByText("Clients"))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
        // Users link is in the hamburger drawer — open it so tests can see and click it
        await Page.Locator("#hamburger-btn").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Users" }))
            .ToBeInViewportAsync(new() { Timeout = 5_000 });
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
public class ProjectDetailAdminTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task NavigateToFirstProject()
    {
        await Page.GotoAsync("/projects");
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
        // Click the first project card to enter the detail page
        await Page.Locator(".mud-card").First.ClickAsync();
        // Wait for the project detail WASM to hydrate (FAB edit button becomes enabled)
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
