using TimeTracker.Contracts.Features.Projects;
using TimeTracker.Contracts.Features.TimeEntries;
using Xunit;

namespace TimeTracker.Tests.Features.Projects;

[Collection("Services")]
public class BudgetCalculationsTests
{
    private static TimeEntryResponse MakeEntry(int projectId, DateTime start, DateTime end) =>
        new(0, new ProjectSummary(projectId, $"Project {projectId}"), start, end, null, null, null);

    private static TimeEntryResponse MakeRunning(int projectId) =>
        new(0, new ProjectSummary(projectId, $"Project {projectId}"), DateTime.Now, null, null, null, null);

    // ── Compute: no budget ──

    [Fact]
    public void Compute_ReturnsNull_WhenBudgetIsNull()
    {
        Assert.Null(BudgetCalculations.Compute(null, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Compute_ReturnsNull_WhenBudgetIsNotPositive(decimal budget)
    {
        Assert.Null(BudgetCalculations.Compute(budget, 10));
    }

    // ── Compute: percentage ──

    [Fact]
    public void Compute_ReportsUsedHoursAndBudget()
    {
        var result = BudgetCalculations.Compute(40m, 10);

        Assert.NotNull(result);
        Assert.Equal(40m, result.BudgetHours);
        Assert.Equal(10, result.UsedHours);
    }

    [Theory]
    [InlineData(40, 0, 0)]
    [InlineData(40, 10, 25)]
    [InlineData(40, 32, 80)]
    [InlineData(40, 40, 100)]
    [InlineData(40, 60, 150)]
    public void Compute_CalculatesPercentUsed(decimal budget, double used, double expectedPercent)
    {
        var result = BudgetCalculations.Compute(budget, used);

        Assert.NotNull(result);
        Assert.Equal(expectedPercent, result.PercentUsed);
    }

    [Fact]
    public void Compute_DoesNotCapPercentUsed_WhenOverBudget()
    {
        var result = BudgetCalculations.Compute(10m, 25);

        Assert.NotNull(result);
        Assert.Equal(250, result.PercentUsed);
    }

    [Fact]
    public void Compute_TreatsNegativeUsedHoursAsZero()
    {
        var result = BudgetCalculations.Compute(10m, -5);

        Assert.NotNull(result);
        Assert.Equal(0, result.UsedHours);
        Assert.Equal(0, result.PercentUsed);
    }

    // ── Compute: state thresholds ──

    [Theory]
    [InlineData(0, BudgetCalculations.BudgetState.UnderBudget)]
    [InlineData(50, BudgetCalculations.BudgetState.UnderBudget)]
    [InlineData(79.5, BudgetCalculations.BudgetState.UnderBudget)]
    [InlineData(80, BudgetCalculations.BudgetState.Warning)]
    [InlineData(99.5, BudgetCalculations.BudgetState.Warning)]
    [InlineData(100, BudgetCalculations.BudgetState.OverBudget)]
    [InlineData(150, BudgetCalculations.BudgetState.OverBudget)]
    public void Compute_SetsStateFromThresholds(double usedHours, BudgetCalculations.BudgetState expected)
    {
        // A 100-hour budget makes used hours and percentage identical.
        var result = BudgetCalculations.Compute(100m, usedHours);

        Assert.NotNull(result);
        Assert.Equal(expected, result.State);
    }

    // ── ProgressValue ──

    [Theory]
    [InlineData(25, 25)]
    [InlineData(100, 100)]
    [InlineData(250, 100)]
    public void ProgressValue_ClampsToOneHundred(double usedHours, double expected)
    {
        var result = BudgetCalculations.Compute(100m, usedHours);

        Assert.NotNull(result);
        Assert.Equal(expected, result.ProgressValue);
    }

    // ── UsedHours ──

    [Fact]
    public void UsedHours_SumsCompletedEntriesForTheProject()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(2026, 1, 1, 9, 0, 0), new DateTime(2026, 1, 1, 11, 0, 0)),
            MakeEntry(1, new DateTime(2026, 1, 2, 9, 0, 0), new DateTime(2026, 1, 2, 10, 30, 0)),
        };

        Assert.Equal(3.5, BudgetCalculations.UsedHours(entries, 1));
    }

    [Fact]
    public void UsedHours_ExcludesOtherProjects()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(2026, 1, 1, 9, 0, 0), new DateTime(2026, 1, 1, 11, 0, 0)),
            MakeEntry(2, new DateTime(2026, 1, 1, 9, 0, 0), new DateTime(2026, 1, 1, 17, 0, 0)),
        };

        Assert.Equal(2, BudgetCalculations.UsedHours(entries, 1));
    }

    [Fact]
    public void UsedHours_ExcludesRunningEntries()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(2026, 1, 1, 9, 0, 0), new DateTime(2026, 1, 1, 11, 0, 0)),
            MakeRunning(1),
        };

        Assert.Equal(2, BudgetCalculations.UsedHours(entries, 1));
    }

    [Fact]
    public void UsedHours_SpansMultipleYears()
    {
        var entries = new List<TimeEntryResponse>
        {
            MakeEntry(1, new DateTime(2025, 12, 31, 9, 0, 0), new DateTime(2025, 12, 31, 12, 0, 0)),
            MakeEntry(1, new DateTime(2026, 1, 1, 9, 0, 0), new DateTime(2026, 1, 1, 11, 0, 0)),
        };

        Assert.Equal(5, BudgetCalculations.UsedHours(entries, 1));
    }

    [Fact]
    public void UsedHours_ReturnsZero_WhenProjectHasNoEntries()
    {
        Assert.Equal(0, BudgetCalculations.UsedHours([], 1));
    }
}
