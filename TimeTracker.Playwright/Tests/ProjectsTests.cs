namespace TimeTracker.Playwright.Tests;

public class ProjectsTests : AuthenticatedPageTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Page.GotoAsync("/projects");
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task ProjectsHeadingIsVisible()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Projects" }).First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ActiveCountChipIsVisible()
    {
        await Expect(Page.GetByText(new Regex(@"\d+ active"))).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AddProjectOpensSheet()
    {
        await Page.Locator(".tt-fab button").ClickAsync();
        await Expect(Page.GetByLabel("Project name")).ToBeVisibleAsync(new() { Timeout = 5_000 });
    }
}
