namespace TimeTracker.Shared.Entities;

public class TimeEntry : SoftDeleteableEntity
{
    public int? ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public DateTime Start { get; set; } = DateTime.Now;
    public DateTime? End { get; set; }
    public string? Note { get; set; }
    public string? InvoiceReference { get; set; }
    public DateTime? InvoicedAt { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>Client-generated idempotency tag for entries created via the offline sync queue
    /// (Stage B). Null for every entry created the normal online way. A filtered unique index
    /// (see TimeTrackerDataContext.OnModelCreating) lets a replayed create — e.g. a retried sync
    /// after a dropped response — return the existing row instead of inserting a duplicate.</summary>
    public Guid? ClientRequestId { get; set; }

    //public AppUser? AppUser { get; set; } = null!;
}

