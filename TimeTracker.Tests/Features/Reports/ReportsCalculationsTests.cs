using TimeTracker.Contracts.Features.Projects;
using TimeTracker.Contracts.Features.Reports;
using TimeTracker.Contracts.Features.TimeEntries;
using Xunit;

namespace TimeTracker.Tests.Features.Reports;

[Collection("Services")]
public class ReportsCalculationsTests
{
    private static readonly int Year = DateTime.Today.Year;

    private static TimeEntryResponse MakeEntry(int projectId, DateTime start, DateTime end,
        string? invoiceRef = null) =>
        new(0, new ProjectSummary(projectId, $"Project {projectId}"), start, end, null, invoiceRef, null);

    private static TimeEntryResponse MakeRunning(int projectId) =>
        new(0, new ProjectSummary(projectId, $"Project {projectId}"), DateTime.Now, null, null, null, null);

    private static ProjectResponse MakeProject(int id, decimal? hourlyRate = 100) =>
        new(id, $"Project {id}", null, null, hourlyRate, null, null, null);

    [Fact]
    public void YtdTotal_SumsAllCompletedEntries()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(Year, 1, 1, 9, 0, 0), new DateTime(Year, 1, 1, 11, 0, 0)),
            MakeEntry(1, new DateTime(Year, 2, 1, 9, 0, 0), new DateTime(Year, 2, 1, 10, 30, 0)),
        };
        var projects = new List<ProjectResponse> { MakeProject(1) };

        var result = ReportsCalculations.Compute(entries, projects);

        Assert.Equal(3.5, result.YtdTotal);
    }

    [Fact]
    public void YtdTotal_ExcludesRunningEntries()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(Year, 1, 1, 9, 0, 0), new DateTime(Year, 1, 1, 11, 0, 0)),
            MakeRunning(1),
        };
        var projects = new List<ProjectResponse> { MakeProject(1) };

        var result = ReportsCalculations.Compute(entries, projects);

        Assert.Equal(2.0, result.YtdTotal);
    }

    [Fact]
    public void UninvoicedTotal_OnlyCountsBillableWithoutInvoiceRef()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(Year, 1, 1, 9, 0, 0), new DateTime(Year, 1, 1, 11, 0, 0)),
            MakeEntry(1, new DateTime(Year, 1, 2, 9, 0, 0), new DateTime(Year, 1, 2, 11, 0, 0), invoiceRef: "INV-001"),
            MakeEntry(2, new DateTime(Year, 1, 3, 9, 0, 0), new DateTime(Year, 1, 3, 11, 0, 0)),
        };
        var projects = new List<ProjectResponse>
        {
            MakeProject(1, hourlyRate: 150),
            MakeProject(2, hourlyRate: null),
        };

        var result = ReportsCalculations.Compute(entries, projects);

        Assert.Equal(2.0, result.UninvoicedTotal);
    }

    [Fact]
    public void MonthlyHours_NonBillable_CountsCorrectMonth()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(Year, 3, 1, 9, 0, 0), new DateTime(Year, 3, 1, 11, 0, 0)),
        };
        var projects = new List<ProjectResponse> { MakeProject(1, hourlyRate: null) };

        var result = ReportsCalculations.Compute(entries, projects);

        Assert.Equal(2.0, result.MonthlyHours[0][2]); // index 0 = non-billable, index 2 = March
        Assert.Equal(0.0, result.MonthlyHours[1][2]); // invoiced = 0
        Assert.Equal(0.0, result.MonthlyHours[2][2]); // uninvoiced = 0
    }

    [Fact]
    public void MonthlyHours_Invoiced_CountsCorrectly()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(Year, 6, 1, 9, 0, 0), new DateTime(Year, 6, 1, 10, 0, 0), invoiceRef: "INV-001"),
        };
        var projects = new List<ProjectResponse> { MakeProject(1, hourlyRate: 100) };

        var result = ReportsCalculations.Compute(entries, projects);

        Assert.Equal(0.0, result.MonthlyHours[0][5]); // non-billable = 0
        Assert.Equal(1.0, result.MonthlyHours[1][5]); // invoiced = 1h
        Assert.Equal(0.0, result.MonthlyHours[2][5]); // uninvoiced = 0
    }

    [Fact]
    public void ProjectRows_OrderedByHoursDescending()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(Year, 1, 1, 9, 0, 0), new DateTime(Year, 1, 1, 10, 0, 0)),
            MakeEntry(2, new DateTime(Year, 1, 1, 9, 0, 0), new DateTime(Year, 1, 1, 12, 0, 0)),
        };
        var projects = new List<ProjectResponse> { MakeProject(1), MakeProject(2) };

        var result = ReportsCalculations.Compute(entries, projects);

        Assert.Equal(2, result.ProjectRows.Count);
        Assert.Equal(2, result.ProjectRows[0].Project.Id);
        Assert.Equal(1, result.ProjectRows[1].Project.Id);
    }

    [Fact]
    public void ProjectRows_ExcludesProjectsWithZeroHours()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(Year, 1, 1, 9, 0, 0), new DateTime(Year, 1, 1, 11, 0, 0)),
        };
        var projects = new List<ProjectResponse> { MakeProject(1), MakeProject(2) };

        var result = ReportsCalculations.Compute(entries, projects);

        Assert.Single(result.ProjectRows);
        Assert.Equal(1, result.ProjectRows[0].Project.Id);
    }

    [Fact]
    public void EmptyEntries_ReturnsZeroTotals()
    {
        var result = ReportsCalculations.Compute([], []);

        Assert.Equal(0.0, result.YtdTotal);
        Assert.Equal(0.0, result.UninvoicedTotal);
        Assert.Empty(result.ProjectRows);
        Assert.Equal(1, result.MaxProjectHours);
    }

    [Fact]
    public void MaxProjectHours_IsLargestProjectHours()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(Year, 1, 1, 9, 0, 0), new DateTime(Year, 1, 1, 11, 0, 0)),
            MakeEntry(2, new DateTime(Year, 1, 1, 9, 0, 0), new DateTime(Year, 1, 1, 14, 0, 0)),
        };
        var projects = new List<ProjectResponse> { MakeProject(1), MakeProject(2) };

        var result = ReportsCalculations.Compute(entries, projects);

        Assert.Equal(5, result.MaxProjectHours);
    }
}
