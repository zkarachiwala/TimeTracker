using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace TimeTracker.Web.Infrastructure;

public static class ConnectionStringBuilder
{
    // Azure SQL free tier: two pools × 30 = 60 max connections (75 limit), MinPoolSize=0 enables auto-pause.
    public static string Build(IConfiguration configuration, bool isDevelopment, string connectionCfgName, string userCfgName, string passwordCfgName)
    {
        var connectionString = configuration.GetConnectionString(connectionCfgName);
        var conStrBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            MinPoolSize = 0,
            MaxPoolSize = 30,
        };
        if (isDevelopment)
        {
            conStrBuilder.UserID = configuration[userCfgName];
            conStrBuilder.Password = configuration[passwordCfgName];
        }
        return conStrBuilder.ConnectionString;
    }

    // EF Core's design-time tooling has no IWebHostEnvironment to consult, so design-time factories
    // derive it from ASPNETCORE_ENVIRONMENT directly. Defaults to Development-like behavior (inject
    // credentials) unless explicitly told "Production" - this mirrors EF's own historical default of
    // treating design-time tooling as Development, which CI deliberately overrides.
    public static bool IsDevelopmentEnvironment(string? aspnetcoreEnvironment) =>
        aspnetcoreEnvironment != "Production";
}
