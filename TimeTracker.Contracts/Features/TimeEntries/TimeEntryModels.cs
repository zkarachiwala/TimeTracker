using System.ComponentModel.DataAnnotations;

namespace TimeTracker.Contracts.Features.TimeEntries;

public record TimeEntryResponse(
    int Id,
    ProjectSummary Project,
    DateTime Start,
    DateTime? End,
    string? Note,
    string? InvoiceReference,
    DateTime? InvoicedAt,
    decimal? EffectiveRate = null,
    bool IsAwardRate = false
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

    /// <summary>Client-generated idempotency tag for the offline sync queue (Stage B). Null for a
    /// normal, already-online create. When set, a replayed create with the same tag returns the
    /// existing row instead of inserting a duplicate.</summary>
    public Guid? ClientRequestId { get; set; }
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

public record DeletedTimeEntryResponse(int Id, string ProjectName, DateTime Start, DateTime? End, string? Note, DateTime? DateDeleted, string? DeletedBy);
