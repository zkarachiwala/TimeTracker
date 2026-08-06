using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TimeTracker.Web.Infrastructure;

namespace TimeTracker.Web.Data;

// dotnet ef prefers this factory over reflecting into Program.cs's host builder, which lets
// design-time tooling authenticate as MigrationsDbUser/MigrationsDbPassword - a separate,
// DDL-capable credential from DbUser/DbPassword, which the running app uses instead. See ADR-035.
public class TimeTrackerDataContextFactory : IDesignTimeDbContextFactory<TimeTrackerDataContext>
{
    public TimeTrackerDataContext CreateDbContext(string[] args)
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
            configuration, isDevelopment, "TimeTrackerConnection", "MigrationsDbUser", "MigrationsDbPassword");

        var optionsBuilder = new DbContextOptionsBuilder<TimeTrackerDataContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new TimeTrackerDataContext(optionsBuilder.Options);
    }
}
