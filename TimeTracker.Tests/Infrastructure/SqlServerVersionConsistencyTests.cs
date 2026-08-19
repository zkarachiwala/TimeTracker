using System.Text.RegularExpressions;
using Xunit;

namespace TimeTracker.Tests.Infrastructure;

/// <summary>
/// Guards against the class of drift described in issue #323: the Testcontainers fixture and the
/// dev container silently running different SQL Server engines.
///
/// RlsIntegrationTests and MigrationSmokeTests are the only automated proof that the security
/// policies behave. If they run on a different engine to the one used for development, that proof
/// is weaker than it looks — and nothing else in the suite would notice.
///
/// This test needs no Docker and runs in the fast loop, so the two can never diverge unnoticed
/// again.
/// </summary>
public class SqlServerVersionConsistencyTests
{
    [Fact]
    public void FixtureImage_MatchesDevContainerImage()
    {
        var composePath = Path.Combine(FindRepositoryRoot(), "docker-compose.yml");
        Assert.True(File.Exists(composePath), $"docker-compose.yml not found at {composePath}");

        var compose = File.ReadAllText(composePath);
        var match = Regex.Match(compose, @"image:\s*(?<image>mcr\.microsoft\.com/mssql/server:\S+)");

        Assert.True(match.Success,
            "Could not find a SQL Server image in docker-compose.yml. If the dev container no " +
            "longer uses one, this guard needs updating.");

        var composeImage = match.Groups["image"].Value;

        Assert.True(composeImage == SqlServerFixture.SqlServerImage,
            $"SQL Server image drift.{Environment.NewLine}" +
            $"  docker-compose.yml:            {composeImage}{Environment.NewLine}" +
            $"  SqlServerFixture.SqlServerImage: {SqlServerFixture.SqlServerImage}{Environment.NewLine}" +
            "Container tests would run against a different engine to the dev container. " +
            "Update both, and docs/testcontainers.md alongside them.");
    }

    /// <summary>
    /// Walks up from the test assembly location until the directory containing the solution is
    /// found, so the test works regardless of build configuration or working directory.
    /// </summary>
    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TimeTracker.sln")))
            dir = dir.Parent;

        Assert.NotNull(dir);
        return dir.FullName;
    }
}
