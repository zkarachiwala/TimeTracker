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
        await Expect(Page).ToHaveTitleAsync(new Regex("Projects"));
    }

    [Test]
    public async Task ProjectsHeadingIsVisible()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Projects" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task ActiveCountChipIsVisible()
    {
        await Expect(Page.GetByText(new Regex(@"\d+ active"))).ToBeVisibleAsync();
    }

    [Test]
    public async Task FabButtonIsVisible()
    {
        var fab = Page.Locator(".tt-fab button");
        await Expect(fab).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddProjectOpensSheet()
    {
        await Page.Locator(".tt-fab button").ClickAsync();
        // Project sheet should open — look for a Name field
        await Expect(Page.GetByLabel(new Regex("name", RegexOptions.IgnoreCase))).ToBeVisibleAsync(new() { Timeout = 5_000 });
    }
}
