using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TimeTracker.Web.Data;

public class TimeTrackerDataContext : DbContext
{
    public TimeTrackerDataContext(DbContextOptions<TimeTrackerDataContext> options) : base(options)
    {        
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
        modelBuilder.Entity<Project>().Property(p => p.HourlyRate).HasPrecision(18, 2);
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();
    public DbSet<TimeTracker.Shared.Entities.Client> Clients => Set<TimeTracker.Shared.Entities.Client>();
}