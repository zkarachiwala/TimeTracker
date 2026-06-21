using TimeTracker.Client.Features.TimeEntries.Components;
using TimeTracker.Client.Shared;
using TimeTracker.ComponentTests.Fixtures;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.ComponentTests.Features.TimeEntries;

public class EntryRowTests : MudBlazorContext
{
    private static TimeEntryResponse MakeEntry(
        int id = 1,
        int projectId = 1,
        string projectName = "My Project",
        DateTime? start = null,
        DateTime? end = null,
        string? note = null,
        string? invoiceRef = null,
        bool isAwardRate = false)
    {
        var s = start ?? DateTime.UtcNow.AddHours(-1);
        var e = end ?? DateTime.UtcNow;
        return new TimeEntryResponse(id, new ProjectSummary(projectId, projectName), s, e, note, invoiceRef, null, null, isAwardRate);
    }

    [Fact]
    public void RendersProjectName_WhenShowProjectIsTrue()
    {
        var entry = MakeEntry(projectName: "Acme Corp");
        var cut = RenderComponent<EntryRow>(p => p
            .Add(e => e.Entry, entry)
            .Add(e => e.ShowProject, true));

        Assert.Contains("Acme Corp", cut.Markup);
    }

    [Fact]
    public void RendersNote_WhenShowProjectIsFalse()
    {
        var entry = MakeEntry(note: "Design review");
        var cut = RenderComponent<EntryRow>(p => p
            .Add(e => e.Entry, entry)
            .Add(e => e.ShowProject, false));

        Assert.Contains("Design review", cut.Markup);
    }

    [Fact]
    public void RendersFallback_WhenShowProjectIsFalseAndNoteIsEmpty()
    {
        var entry = MakeEntry(note: null);
        var cut = RenderComponent<EntryRow>(p => p
            .Add(e => e.Entry, entry)
            .Add(e => e.ShowProject, false));

        Assert.Contains("Time entry", cut.Markup);
    }

    [Fact]
    public void RendersDuration_InHoursAndMinutes_WhenOver60Minutes()
    {
        var start = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 10, 30, 0, DateTimeKind.Utc);
        var entry = MakeEntry(start: start, end: end);
        var cut = RenderComponent<EntryRow>(p => p.Add(e => e.Entry, entry));

        Assert.Contains("1h 30m", cut.Markup);
    }

    [Fact]
    public void RendersDuration_InMinutesOnly_WhenUnder60Minutes()
    {
        var start = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 9, 45, 0, DateTimeKind.Utc);
        var entry = MakeEntry(start: start, end: end);
        var cut = RenderComponent<EntryRow>(p => p.Add(e => e.Entry, entry));

        // Check the duration text element specifically — checking cut.Markup would match
        // SVG path data (e.g. d="M0 0h24v24H0z") which also contains "h" substrings.
        var durationEl = cut.Find("[style*='tabular-nums']");
        Assert.Equal("45m", durationEl.TextContent.Trim());
    }

    [Fact]
    public void RendersInvoiceIcon_WhenInvoiceReferenceIsSet()
    {
        var entry = MakeEntry(invoiceRef: "INV-2026-001");
        var cut = RenderComponent<EntryRow>(p => p.Add(e => e.Entry, entry));

        // MudTooltip renders its text in the popover portal, not inline in the component —
        // check the component instance's Text parameter instead.
        var tooltip = cut.FindComponent<MudTooltip>();
        Assert.Equal("Invoiced: INV-2026-001", tooltip.Instance.Text);
    }

    [Fact]
    public void DoesNotRenderInvoiceIcon_WhenNoInvoiceReference()
    {
        var entry = MakeEntry(invoiceRef: null, isAwardRate: false);
        var cut = RenderComponent<EntryRow>(p => p.Add(e => e.Entry, entry));

        // No tooltips should be rendered when neither invoice ref nor award rate is set
        Assert.Empty(cut.FindComponents<MudTooltip>());
    }

    [Fact]
    public void RendersAwardRateChip_WhenIsAwardRateIsTrue()
    {
        var entry = MakeEntry(isAwardRate: true);
        var cut = RenderComponent<EntryRow>(p => p.Add(e => e.Entry, entry));

        Assert.Contains("AW", cut.Markup);
    }

    [Fact]
    public void DoesNotRenderAwardRateChip_WhenIsAwardRateIsFalse()
    {
        var entry = MakeEntry(isAwardRate: false);
        var cut = RenderComponent<EntryRow>(p => p.Add(e => e.Entry, entry));

        Assert.DoesNotContain("AW", cut.Markup);
    }

    [Fact]
    public void AppliesProjectColour_FromProjectId()
    {
        var entry = MakeEntry(projectId: 1);
        var cut = RenderComponent<EntryRow>(p => p.Add(e => e.Entry, entry));

        var expectedColour = ProjectColors.ForProject(1);
        Assert.Contains(expectedColour, cut.Markup);
    }

    [Fact]
    public void InvokesOnEdit_WhenRowIsClicked()
    {
        TimeEntryResponse? received = null;
        var entry = MakeEntry();
        var cut = RenderComponent<EntryRow>(p => p
            .Add(e => e.Entry, entry)
            .Add(e => e.OnEdit, EventCallback.Factory.Create<TimeEntryResponse>(this, r => received = r)));

        cut.Find("div").Click();

        Assert.Equal(entry, received);
    }
}
