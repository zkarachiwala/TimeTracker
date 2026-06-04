namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class ReportsTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task NavigateToReports()
    {
        await Page.GotoAsync("/reports");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30_000 });
    }

    [Test]
    public async Task ReportsPageLoads()
    {
        await Expect(Page).ToHaveTitleAsync(new Regex("Reports"));
    }

    [Test]
    public async Task ReportsPageDoesNotShowError()
    {
        await Expect(Page.GetByText(new Regex("error|exception|500", RegexOptions.IgnoreCase))).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task ReportsContentIsVisible()
    {
        // Reports page should show something — either chart content or an empty-state message
        var hasContent = await Page.Locator(".mud-card, .mud-chart, canvas").CountAsync() > 0;
        var hasEmptyState = await Page.GetByText(new Regex("no data|no entries|nothing", RegexOptions.IgnoreCase)).IsVisibleAsync();
        Assert.That(hasContent || hasEmptyState, Is.True,
            "Reports page should render chart content or an empty-state message");
    }
}
