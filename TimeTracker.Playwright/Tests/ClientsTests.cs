namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class ClientsTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task NavigateToClients()
    {
        await Page.GotoAsync("/clients");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30_000 });
    }

    [Test]
    public async Task ClientsPageLoads()
    {
        await Expect(Page).ToHaveTitleAsync(new Regex("Clients"));
    }

    [Test]
    public async Task ClientsHeadingIsVisible()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Clients" })).ToBeVisibleAsync();
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
    public async Task AddClientOpensSheet()
    {
        await Page.Locator(".tt-fab button").ClickAsync();
        await Expect(Page.GetByLabel(new Regex("name", RegexOptions.IgnoreCase))).ToBeVisibleAsync(new() { Timeout = 5_000 });
    }
}
