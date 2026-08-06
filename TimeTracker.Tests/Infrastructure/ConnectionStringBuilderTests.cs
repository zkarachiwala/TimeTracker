using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using TimeTracker.Web.Infrastructure;
using Xunit;

namespace TimeTracker.Tests.Infrastructure;

public class ConnectionStringBuilderTests
{
    private static IConfiguration BuildConfig(params (string Key, string? Value)[] pairs) =>
        new ConfigurationBuilder().AddInMemoryCollection(
            pairs.ToDictionary(p => p.Key, p => p.Value)).Build();

    [Fact]
    public void Build_InjectsCredentials_WhenIsDevelopment()
    {
        var configuration = BuildConfig(
            ("ConnectionStrings:Conn", "Server=localhost;Database=Db"),
            ("User", "app_user"),
            ("Password", "app_password"));

        var result = ConnectionStringBuilder.Build(configuration, isDevelopment: true, "Conn", "User", "Password");

        var builder = new SqlConnectionStringBuilder(result);
        Assert.Equal("app_user", builder.UserID);
        Assert.Equal("app_password", builder.Password);
    }

    [Fact]
    public void Build_DoesNotInjectCredentials_WhenNotDevelopment()
    {
        var configuration = BuildConfig(
            ("ConnectionStrings:Conn", "Server=localhost;Database=Db"),
            ("User", "app_user"),
            ("Password", "app_password"));

        var result = ConnectionStringBuilder.Build(configuration, isDevelopment: false, "Conn", "User", "Password");

        var builder = new SqlConnectionStringBuilder(result);
        Assert.Equal(string.Empty, builder.UserID);
        Assert.Equal(string.Empty, builder.Password);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Build_AlwaysSetsFreeTierPoolLimits(bool isDevelopment)
    {
        var configuration = BuildConfig(
            ("ConnectionStrings:Conn", "Server=localhost;Database=Db"),
            ("User", "app_user"),
            ("Password", "app_password"));

        var result = ConnectionStringBuilder.Build(configuration, isDevelopment, "Conn", "User", "Password");

        var builder = new SqlConnectionStringBuilder(result);
        Assert.Equal(0, builder.MinPoolSize);
        Assert.Equal(30, builder.MaxPoolSize);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("Development", true)]
    [InlineData("Staging", true)]
    [InlineData("Production", false)]
    public void IsDevelopmentEnvironment_DefaultsTrueUnlessExplicitlyProduction(string? aspnetcoreEnvironment, bool expected)
    {
        Assert.Equal(expected, ConnectionStringBuilder.IsDevelopmentEnvironment(aspnetcoreEnvironment));
    }
}
