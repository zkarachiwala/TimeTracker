using TimeTracker.Client.Features.Projects.Components;
using TimeTracker.Client.Shared;
using TimeTracker.ComponentTests.Fixtures;
using TimeTracker.Contracts.Features.Projects;

namespace TimeTracker.ComponentTests.Features.Projects;

public class ProjectCardTests : MudBlazorContext
{
    private static ProjectResponse MakeProject(
        int id = 1,
        string name = "Test Project",
        string? clientName = null,
        decimal? hourlyRate = null,
        DateTime? startDate = null) =>
        new(id, name, null, clientName, hourlyRate, null, startDate, null);

    [Fact]
    public void RendersProjectName()
    {
        var project = MakeProject(name: "Acme Website");
        var cut = Render<ProjectCard>(p => p.Add(c => c.Project, project));

        Assert.Contains("Acme Website", cut.Markup);
    }

    [Fact]
    public void RendersClientName_WhenSet()
    {
        var project = MakeProject(clientName: "Acme Corp");
        var cut = Render<ProjectCard>(p => p.Add(c => c.Project, project));

        Assert.Contains("Acme Corp", cut.Markup);
    }

    [Fact]
    public void RendersNoClient_WhenClientNameIsNull()
    {
        var project = MakeProject(clientName: null);
        var cut = Render<ProjectCard>(p => p.Add(c => c.Project, project));

        Assert.Contains("No client", cut.Markup);
    }

    [Fact]
    public void RendersHourlyRateChip_WhenRateIsSet()
    {
        var project = MakeProject(hourlyRate: 150m);
        var cut = Render<ProjectCard>(p => p.Add(c => c.Project, project));

        Assert.Contains("150", cut.Markup);
        Assert.Contains("/h", cut.Markup);
    }

    [Fact]
    public void RendersInternalChip_WhenRateIsNull()
    {
        var project = MakeProject(hourlyRate: null);
        var cut = Render<ProjectCard>(p => p.Add(c => c.Project, project));

        Assert.Contains("internal", cut.Markup);
    }

    [Fact]
    public void RendersInternalChip_WhenRateIsZero()
    {
        var project = MakeProject(hourlyRate: 0m);
        var cut = Render<ProjectCard>(p => p.Add(c => c.Project, project));

        Assert.Contains("internal", cut.Markup);
    }

    [Fact]
    public void RendersStartDate_WhenSet()
    {
        var project = MakeProject(startDate: new DateTime(2025, 3, 1));
        var cut = Render<ProjectCard>(p => p.Add(c => c.Project, project));

        Assert.Contains("Mar 2025", cut.Markup);
    }

    [Fact]
    public void RendersDashPlaceholder_WhenStartDateIsNull()
    {
        var project = MakeProject(startDate: null);
        var cut = Render<ProjectCard>(p => p.Add(c => c.Project, project));

        Assert.Contains("since —", cut.Markup);
    }

    [Fact]
    public void AppliesProjectColour_FromProjectId()
    {
        var project = MakeProject(id: 3);
        var cut = Render<ProjectCard>(p => p.Add(c => c.Project, project));

        var expectedColour = ProjectColors.ForProject(3);
        Assert.Contains(expectedColour, cut.Markup);
    }

    [Fact]
    public void RendersYtdHours_WhenProvided()
    {
        var project = MakeProject();
        var cut = Render<ProjectCard>(p => p
            .Add(c => c.Project, project)
            .Add(c => c.YtdHours, 42));

        Assert.Contains("42 h logged YTD", cut.Markup);
    }

    [Fact]
    public void InvokesOnOpen_WithProjectId_WhenCardIsClicked()
    {
        int? receivedId = null;
        var project = MakeProject(id: 7);
        var cut = Render<ProjectCard>(p => p
            .Add(c => c.Project, project)
            .Add(c => c.OnOpen, EventCallback.Factory.Create<int>(this, id => receivedId = id)));

        cut.Find(".mud-card").Click();

        Assert.Equal(7, receivedId);
    }
}
