using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TimeTracker.API.Data;

public class TimeTrackerDataContext : DbContext
{
    public TimeTrackerDataContext(DbContextOptions<TimeTrackerDataContext> options) : base(options)
    {        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema( "app" );
        modelBuilder.Entity<TimeEntry>().Navigation(c => c.Project).AutoInclude();
        modelBuilder.Entity<Project>().Navigation(c => c.ProjectDetails).AutoInclude();
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectDetails> ProjectDetails => Set<ProjectDetails>();
}