namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class TimeEntriesTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task NavigateToEntries()
    {
        await Page.GotoAsync("/entries");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30_000 });
    }

    [Test]
    public async Task EntriesPageLoads()
    {
        await Expect(Page).ToHaveTitleAsync(new Regex("Entries"));
    }

    [Test]
    public async Task FilterTabsAreVisible()
    {
        foreach (var tab in new[] { "Day", "Month", "Year", "Project" })
        {
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = tab })).ToBeVisibleAsync();
        }
    }

    [Test]
    public async Task SummaryCardIsVisible()
    {
        await Expect(Page.GetByText("Total tracked")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DateStepperIsVisible()
    {
        // The range stepper card contains chevron buttons — verify they are present
        await Expect(Page.Locator(".mud-icon-button").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task StepBackChangesDateLabel()
    {
        var label = Page.Locator(".mud-typography-subtitle1").First;
        var initialText = await label.InnerTextAsync();

        // First chevron button is "step back"
        await Page.Locator(".mud-icon-button").First.ClickAsync();
        var afterBack = await label.InnerTextAsync();

        Assert.That(afterBack, Is.Not.EqualTo(initialText), "Date label should change after stepping back");
    }

    [Test]
    public async Task MonthTabShowsMonthLabel()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Month" }).ClickAsync();
        // Month view label is "MMMM yyyy" — use GetByText with regex to find it
        await Expect(Page.GetByText(new Regex(@"[A-Z][a-z]+ \d{4}")))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task ProjectTabShowsProjectDropdown()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Project" }).ClickAsync();
        // MudSelect renders a label element; verify it's visible
        await Expect(Page.Locator("label").Filter(new() { HasText = "Project" }))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });
    }

    [Test]
    public async Task FabButtonIsVisible()
    {
        await Expect(Page.Locator(".tt-fab button")).ToBeVisibleAsync();
    }
}
