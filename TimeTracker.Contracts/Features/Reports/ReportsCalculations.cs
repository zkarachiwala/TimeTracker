using TimeTracker.Contracts.Features.Projects;
using TimeTracker.Contracts.Features.TimeEntries;

namespace TimeTracker.Contracts.Features.Reports;

public static class ReportsCalculations
{
    public record ProjectRow(ProjectResponse Project, int Hours, decimal Revenue);

    public record ReportsData(
        double YtdTotal,
        double UninvoicedTotal,
        double[][] MonthlyHours,
        List<ProjectRow> ProjectRows,
        int MaxProjectHours,
        decimal YtdEarned,
        decimal UninvoicedAmount
    );

    public static ReportsData Compute(List<TimeEntryResponse> entries, List<ProjectResponse> projects)
    {
        var completed = entries.Where(e => e.End.HasValue).ToList();

        var ytdTotal = Math.Round(completed.Sum(e => (e.End!.Value - e.Start).TotalHours), 1);

        var billableIds = projects.Where(p => p.HourlyRate is > 0).Select(p => p.Id).ToHashSet();
        var projectRates = projects.ToDictionary(p => p.Id, p => p.HourlyRate);

        // Revenue uses EffectiveRate (award rate where applicable) falling back to project HourlyRate.
        // Invoiced revenue uses current HourlyRate since the rate at invoice time is not stored.
        decimal EntryRevenue(TimeEntryResponse e)
        {
            var hours = (decimal)(e.End!.Value - e.Start).TotalHours;
            var rate = e.EffectiveRate ?? (projectRates.GetValueOrDefault(e.Project.Id) ?? 0);
            return hours * rate;
        }

        var uninvoicedTotal = Math.Round(
            completed.Where(e => billableIds.Contains(e.Project.Id) && string.IsNullOrEmpty(e.InvoiceReference))
                     .Sum(e => (e.End!.Value - e.Start).TotalHours), 1);

        var ytdEarned = Math.Round(completed.Sum(EntryRevenue), 2);

        var uninvoicedAmount = Math.Round(
            completed.Where(e => string.IsNullOrEmpty(e.InvoiceReference))
                     .Sum(EntryRevenue), 2);

        double MonthHours(int m, Func<TimeEntryResponse, bool> predicate) =>
            Math.Round(completed.Where(e => e.Start.Month == m && predicate(e))
                                .Sum(e => (e.End!.Value - e.Start).TotalHours), 1);

        var monthlyHours = new double[3][];
        monthlyHours[0] = Enumerable.Range(1, 12)
            .Select(m => MonthHours(m, e => !billableIds.Contains(e.Project.Id)))
            .ToArray();
        monthlyHours[1] = Enumerable.Range(1, 12)
            .Select(m => MonthHours(m, e => billableIds.Contains(e.Project.Id) && !string.IsNullOrEmpty(e.InvoiceReference)))
            .ToArray();
        monthlyHours[2] = Enumerable.Range(1, 12)
            .Select(m => MonthHours(m, e => billableIds.Contains(e.Project.Id) && string.IsNullOrEmpty(e.InvoiceReference)))
            .ToArray();

        var projectRows = projects
            .Select(p => new ProjectRow(p,
                (int)Math.Round(completed
                    .Where(e => e.Project.Id == p.Id)
                    .Sum(e => (e.End!.Value - e.Start).TotalHours)),
                Math.Round(completed
                    .Where(e => e.Project.Id == p.Id)
                    .Sum(EntryRevenue), 2)))
            .Where(r => r.Hours > 0)
            .OrderByDescending(r => r.Hours)
            .ToList();

        var maxProjectHours = projectRows.Count > 0 ? projectRows.Max(r => r.Hours) : 1;

        return new ReportsData(ytdTotal, uninvoicedTotal, monthlyHours, projectRows, maxProjectHours, ytdEarned, uninvoicedAmount);
    }
}
