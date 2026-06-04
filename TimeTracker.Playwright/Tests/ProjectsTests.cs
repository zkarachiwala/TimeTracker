namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class ProjectsTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task NavigateToProjects()
    {
        await Page.GotoAsync("/projects");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30_000 });
    }

    [Test]
    public async Task ProjectsPageLoads()
    {
        await Expect(Page).ToHaveURLAsync(new Regex("/projects"));
    }

    [Test]
    public async Task ProjectsHeadingIsVisible()
    {
        // Heading appears in both toolbar and page — take the first
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Projects" }).First).ToBeVisibleAsync();
    }

    [Test]
    public async Task ActiveCountChipIsVisible()
    {
        await Expect(Page.GetByText(new Regex(@"\d+ active"))).ToBeVisibleAsync();
    }

    [Test]
    public async Task FabButtonIsVisible()
    {
        await Expect(Page.Locator(".tt-fab button")).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddProjectOpensSheet()
    {
        await Page.Locator(".tt-fab button").ClickAsync();
        await Expect(Page.GetByLabel("Project name")).ToBeVisibleAsync(new() { Timeout = 5_000 });
    }
}
