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
//        modelBuilder.Entity<TimeEntry>().Navigation(c => c.AppUser).AutoInclude();
        modelBuilder.Entity<Project>().Navigation(c => c.ProjectDetails).AutoInclude();
        modelBuilder.Entity<Project>().Navigation(c => c.ProjectUsers).AutoInclude();
        modelBuilder.Entity<Project>().HasMany(p => p.ProjectUsers).WithOne(pu => pu.Project).HasForeignKey(pu => pu.ProjectId).IsRequired();
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectDetails> ProjectDetails => Set<ProjectDetails>();
    public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();
}