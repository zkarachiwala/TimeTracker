using TimeTracker.Client.Features.TimeEntries.Components;
using TimeTracker.ComponentTests.Fixtures;

namespace TimeTracker.ComponentTests.Features.TimeEntries;

public class CalendarViewTests : MudBlazorContext
{
    // Covers the event wiring Heron → CalendarView → OnDaySelected (#432). Whether the whole cell is
    // clickable is a CSS concern (app.css stretched link) and is covered by the Playwright test.
    [Fact]
    public void ClickingDayLink_RaisesOnDaySelected_WithThatDate()
    {
        DateTime? selected = null;
        var cut = Render<CalendarView>(p => p
            .Add(c => c.CurrentMonth, new DateTime(2026, 10, 17))
            .Add(c => c.OnDaySelected, (DateTime d) => selected = d));

        // Day-number links in the current month (outside-month days carry mud-cal-month-outside)
        var link = cut.FindAll("a.mud-cal-month-cell-link")
            .Single(a => a.TextContent.Trim() == "17" && a.QuerySelector(".mud-cal-month-outside") is null);
        link.Click();

        Assert.Equal(new DateTime(2026, 10, 17), selected?.Date);
    }
}
