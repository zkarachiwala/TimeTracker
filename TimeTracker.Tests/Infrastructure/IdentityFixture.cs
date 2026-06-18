using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Shared.Entities;
using TimeTracker.Web.Data;

namespace TimeTracker.Tests.Infrastructure;

public sealed class IdentityFixture : IDisposable
{
    private readonly ServiceProvider _provider;

    public UserManager<User> UserManager => _provider.GetRequiredService<UserManager<User>>();
    public RoleManager<IdentityRole> RoleManager => _provider.GetRequiredService<RoleManager<IdentityRole>>();
    public IdentityDataContext Db => _provider.GetRequiredService<IdentityDataContext>();

    public IdentityFixture(params (string Key, string Value)[] configEntries)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<IdentityDataContext>(o =>
            o.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddIdentity<User, IdentityRole>(o => o.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<IdentityDataContext>();

        var configValues = configEntries.ToDictionary(e => e.Key, e => (string?)e.Value);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build());

        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();
}
