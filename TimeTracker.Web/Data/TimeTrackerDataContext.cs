using System.Security.Claims;

namespace TimeTracker.Web.Data;

public class TimeTrackerDataContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public TimeTrackerDataContext(
        DbContextOptions<TimeTrackerDataContext> options,
        IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");
        modelBuilder.Entity<TimeEntry>().Navigation(c => c.Project).AutoInclude();
        modelBuilder.Entity<Project>().Navigation(c => c.ProjectUsers).AutoInclude();
        modelBuilder.Entity<Project>().Navigation(c => c.Client).AutoInclude();
        modelBuilder.Entity<Project>().HasMany(p => p.ProjectUsers).WithOne(pu => pu.Project).HasForeignKey(pu => pu.ProjectId).IsRequired();
        modelBuilder.Entity<Project>().HasOne(p => p.Client).WithMany(c => c.Projects).HasForeignKey(p => p.ClientId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<TimeTracker.Shared.Entities.Client>().HasIndex(c => c.Name).IsUnique();
        modelBuilder.Entity<TimeTracker.Shared.Entities.Client>().Property(c => c.DefaultHourlyRate).HasPrecision(18, 2);
        modelBuilder.Entity<TimeTracker.Shared.Entities.Client>().Property(c => c.AwardRate).HasPrecision(18, 2);
        modelBuilder.Entity<Project>().Property(p => p.HourlyRate).HasPrecision(18, 2);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var userId = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedBy = userId;

            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedBy = userId;
        }

        foreach (var entry in ChangeTracker.Entries<SoftDeleteableEntity>())
        {
            if (entry.State == EntityState.Modified
                && entry.Entity.IsDeleted
                && entry.OriginalValues.GetValue<bool>(nameof(SoftDeleteableEntity.IsDeleted)) == false)
            {
                entry.Entity.DeletedBy = userId;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ProjectUser>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedBy = userId;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();
    public DbSet<TimeTracker.Shared.Entities.Client> Clients => Set<TimeTracker.Shared.Entities.Client>();
}
