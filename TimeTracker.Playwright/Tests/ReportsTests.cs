namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class ReportsTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task NavigateToReports()
    {
        await Page.GotoAsync("/reports");
        await Expect(Page.GetByText("YTD hours")).ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    [Test]
    public async Task ReportsContentIsVisible()
    {
        await Expect(Page.GetByText("Hours by month")).ToBeVisibleAsync();
    }
}
