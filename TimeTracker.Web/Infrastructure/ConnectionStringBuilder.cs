using Microsoft.Data.SqlClient;

namespace TimeTracker.Web.Infrastructure;

public static class ConnectionStringBuilder
{
    // Azure SQL free tier: two pools × 30 = 60 max connections (75 limit), MinPoolSize=0 enables auto-pause.
    public static string Build(WebApplicationBuilder builder, string connectionCfgName, string userCfgName, string passwordCfgName)
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionCfgName);
        var conStrBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            MinPoolSize = 0,
            MaxPoolSize = 30,
        };
        if (builder.Environment.IsDevelopment())
        {
            conStrBuilder.UserID = builder.Configuration[userCfgName];
            conStrBuilder.Password = builder.Configuration[passwordCfgName];
        }
        return conStrBuilder.ConnectionString;
    }
}
