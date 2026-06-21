namespace TimeTracker.Playwright.Tests;

public class ReportsTests : AuthenticatedPageTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Page.GotoAsync("/reports");
        await Expect(Page.GetByText("YTD hours")).ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task ReportsContentIsVisible()
    {
        await Expect(Page.GetByText("Hours by month")).ToBeVisibleAsync();
    }
}
