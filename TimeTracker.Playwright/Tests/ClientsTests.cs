namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class ClientsTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task NavigateToClients()
    {
        await Page.GotoAsync("/clients");
        await Expect(Page.Locator(".tt-fab button")).ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    [Test]
    public async Task ClientsHeadingIsVisible()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Clients" }).First).ToBeVisibleAsync();
    }

    [Test]
    public async Task ActiveCountChipIsVisible()
    {
        await Expect(Page.GetByText(new Regex(@"\d+ active"))).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddClientOpensSheet()
    {
        await Page.Locator(".tt-fab button").ClickAsync();
        await Expect(Page.GetByLabel("Client name")).ToBeVisibleAsync(new() { Timeout = 5_000 });
    }
}
