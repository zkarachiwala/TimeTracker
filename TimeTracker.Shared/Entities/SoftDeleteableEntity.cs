namespace TimeTracker.Shared.Entities;

public class SoftDeleteableEntity : BaseEntity
{
    public bool IsDeleted { get; set; } = false;
    public DateTime? DateDeleted { get; set; }
}