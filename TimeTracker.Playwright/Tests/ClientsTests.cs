namespace TimeTracker.Playwright.Tests;

public class ClientsTests : AuthenticatedPageTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Page.GotoAsync("/clients");
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task ClientsHeadingIsVisible()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Clients" }).First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ActiveCountChipIsVisible()
    {
        await Expect(Page.GetByText(new Regex(@"\d+ active"))).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AddClientOpensSheet()
    {
        await Page.Locator(".tt-fab button").ClickAsync();
        await Expect(Page.GetByLabel("Client name")).ToBeVisibleAsync(new() { Timeout = 5_000 });
    }
}
