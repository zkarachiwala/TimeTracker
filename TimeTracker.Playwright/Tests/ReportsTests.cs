namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class ReportsTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task NavigateToReports()
    {
        await Page.GotoAsync("/reports");
        // KPI cards are always rendered after Blazor connects
        await Expect(Page.GetByText("YTD hours")).ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    [Test]
    public async Task ReportsPageLoads()
    {
        await Expect(Page).ToHaveURLAsync(new Regex("/reports"));
    }

    [Test]
    public async Task ReportsPageDoesNotShowError()
    {
        await Expect(Page.GetByText(new Regex("error|exception|500", RegexOptions.IgnoreCase))).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task ReportsContentIsVisible()
    {
        // KPI cards are always rendered regardless of data — use them as the Blazor-ready signal
        await Expect(Page.GetByText("YTD hours")).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Expect(Page.GetByText("Hours by month")).ToBeVisibleAsync();
    }
}
