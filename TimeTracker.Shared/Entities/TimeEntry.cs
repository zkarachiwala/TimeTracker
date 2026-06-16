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
    //public AppUser? AppUser { get; set; } = null!;
}

