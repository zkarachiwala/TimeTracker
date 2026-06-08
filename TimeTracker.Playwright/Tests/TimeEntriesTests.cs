namespace TimeTracker.Playwright.Tests;

[TestFixture]
public class TimeEntriesTests : AuthenticatedPageTest
{
    [SetUp]
    public async Task NavigateToEntries()
    {
        await Page.GotoAsync("/entries");
        await Expect(Page.Locator(".tt-fab button")).ToBeEnabledAsync(new() { Timeout = 30_000 });
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
        var label = Page.GetByText(new Regex(@"Today|Yesterday|Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday"));
        await Expect(label.Locator("..").Locator(".mud-icon-button").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task StepBackChangesDateLabel()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Month" }).ClickAsync();

        var label = Page.GetByText(new Regex(@"[A-Z][a-z]+ \d{4}"));
        await Expect(label).ToBeVisibleAsync();
        var initialText = await label.InnerTextAsync();

        var backButton = label.Locator("..").Locator(".mud-icon-button").First;
        await backButton.ClickAsync();

        await Expect(label).Not.ToHaveTextAsync(initialText, new() { Timeout = 5_000 });
    }

    [Test]
    public async Task MonthTabShowsMonthLabel()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Month" }).ClickAsync();
        await Expect(Page.GetByText(new Regex(@"[A-Z][a-z]+ \d{4}")))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task ProjectTabShowsProjectDropdown()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Project" }).ClickAsync();
        await Expect(Page.Locator("label").Filter(new() { HasText = "Project" }))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });
    }
}
