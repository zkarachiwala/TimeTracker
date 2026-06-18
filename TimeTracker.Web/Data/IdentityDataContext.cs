using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TimeTracker.Web.Data;

public class IdentityDataContext : IdentityDbContext<User>
{
    public IdentityDataContext(DbContextOptions<IdentityDataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("id");
        base.OnModelCreating(modelBuilder);
    }
}
