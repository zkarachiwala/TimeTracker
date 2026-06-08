using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Contracts.Features.TimeEntries;

public record TimeEntryResponse(
    int Id,
    ProjectSummary Project,
    DateTime Start,
    DateTime? End,
    string? Note,
    string? InvoiceReference,
    DateTime? InvoicedAt
);

public record ProjectSummary(int Id, string Name);

public class TimeEntryRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Please select a project.")]
    public int ProjectId { get; set; }
    public DateTime Start { get; set; } = DateTime.Now;
    public DateTime? End { get; set; }
    public string? Note { get; set; }
}

public class TimeEntryCreateRequest
{
    public int ProjectId { get; set; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public string? Note { get; set; }
}

public class TimeEntryUpdateRequest
{
    public int? ProjectId { get; set; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public string? Note { get; set; }
    public string? InvoiceReference { get; set; }
    public DateTime? InvoicedAt { get; set; }
}

public class TimeEntryResponseWrapper
{
    public List<TimeEntryResponse> TimeEntries { get; init; } = new();
    public int Count { get; init; }
    public TimeSpan TotalDuration { get; init; }
}
