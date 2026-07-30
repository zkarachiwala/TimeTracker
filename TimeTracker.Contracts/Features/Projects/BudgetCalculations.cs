using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Contracts.Features.Projects;

public static class BudgetCalculations
{
    /// <summary>Warn once usage reaches this share of the budget.</summary>
    public const double WarningThresholdPercent = 80d;

    public enum BudgetState { UnderBudget, Warning, OverBudget }

    /// <param name="PercentUsed">Uncapped, so an overrun reads as e.g. 120. Clamp before binding to a progress bar.</param>
    public record BudgetStatus(decimal BudgetHours, double UsedHours, double PercentUsed, BudgetState State)
    {
        public double ProgressValue => Math.Clamp(PercentUsed, 0d, 100d);
    }

    /// <summary>
    /// Returns null when the project has no budget — budgets are optional, and a project billed
    /// as plain hours simply has nothing to show.
    /// </summary>
    public static BudgetStatus? Compute(decimal? budgetHours, double usedHours)
    {
        if (budgetHours is not > 0) return null;

        var budget = budgetHours.Value;
        var used = Math.Max(usedHours, 0d);
        var percent = Math.Round(used / (double)budget * 100d, 1);

        var state = percent >= 100d ? BudgetState.OverBudget
            : percent >= WarningThresholdPercent ? BudgetState.Warning
            : BudgetState.UnderBudget;

        return new BudgetStatus(budget, Math.Round(used, 1), percent, state);
    }

    /// <summary>Total hours logged against a project across all years. Running entries are excluded.</summary>
    public static double UsedHours(IEnumerable<TimeEntryResponse> entries, int projectId) =>
        Math.Round(entries
            .Where(e => e.Project.Id == projectId && e.End.HasValue)
            .Sum(e => (e.End!.Value - e.Start).TotalHours), 1);
}
