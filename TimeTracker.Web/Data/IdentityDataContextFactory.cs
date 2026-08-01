using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TimeTracker.Web.Infrastructure;

namespace TimeTracker.Web.Data;

// See TimeTrackerDataContextFactory - same rationale (ADR-035).
public class IdentityDataContextFactory : IDesignTimeDbContextFactory<IdentityDataContext>
{
    public IdentityDataContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var isDevelopment = ConnectionStringBuilder.IsDevelopmentEnvironment(environment);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = ConnectionStringBuilder.Build(
            configuration, isDevelopment, "IdentityConnection", "MigrationsDbUser", "MigrationsDbPassword");

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDataContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new IdentityDataContext(optionsBuilder.Options);
    }
}
