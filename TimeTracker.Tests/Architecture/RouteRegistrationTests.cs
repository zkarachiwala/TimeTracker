using System.Reflection;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace TimeTracker.Tests.Architecture;

public class RouteRegistrationTests
{
    [Fact]
    public void NoAssembliesHaveDuplicatePageRoutes()
    {
        var assemblies = new[]
        {
            typeof(TimeTracker.Web.App).Assembly,
            typeof(TimeTracker.Client.Routes).Assembly,
        };

        var duplicates = assemblies
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => t.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>())
            .Select(r => r.Template)
            .GroupBy(r => r)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate @page routes found across assemblies: {string.Join(", ", duplicates)}");
    }
}
