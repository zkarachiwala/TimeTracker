namespace TimeTracker.API.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {        
    }

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<Project> Projects => Set<Project>();
}