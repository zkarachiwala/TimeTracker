using Microsoft.AspNetCore.Identity;
using TimeTracker.Shared.Entities;
using TimeTracker.Tests.Infrastructure;
using TimeTracker.Web.Features.Admin;
using Xunit;

namespace TimeTracker.Tests.Features.Admin;

[Collection("Services")]
public class UserManagementServiceTests
{
    private const string Email1 = "alice@example.com";
    private const string Email2 = "bob@example.com";

    private static async Task<(UserManagementService Service, IdentityFixture Fixture)> BuildAsync()
    {
        var fixture = new IdentityFixture();
        await fixture.RoleManager.CreateAsync(new IdentityRole("Admin"));
        return (new UserManagementService(fixture.UserManager), fixture);
    }

    private static async Task<User> CreateUserAsync(IdentityFixture fixture, string email)
    {
        var user = new User { UserName = email, Email = email, EmailConfirmed = true };
        await fixture.UserManager.CreateAsync(user);
        return user;
    }

    [Fact]
    public async Task GetUsers_returns_all_registered_users()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            await CreateUserAsync(fixture, Email1);
            await CreateUserAsync(fixture, Email2);

            var users = await svc.GetUsersAsync();

            Assert.Equal(2, users.Count);
            Assert.Contains(users, u => u.Email == Email1);
            Assert.Contains(users, u => u.Email == Email2);
        }
    }

    [Fact]
    public async Task GetUsers_reflects_admin_status()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            var user = await CreateUserAsync(fixture, Email1);
            await fixture.UserManager.AddToRoleAsync(user, "Admin");

            var users = await svc.GetUsersAsync();

            Assert.True(users.Single(u => u.Email == Email1).IsAdmin);
        }
    }

    [Fact]
    public async Task SetAdminRole_promotes_user_to_admin()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            var user = await CreateUserAsync(fixture, Email1);

            var result = await svc.SetAdminRoleAsync(user.Id, true);

            Assert.Equal(SetAdminRoleResult.Success, result);
            Assert.True(await fixture.UserManager.IsInRoleAsync(user, "Admin"));
        }
    }

    [Fact]
    public async Task SetAdminRole_demotes_user_from_admin_when_another_admin_exists()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            var alice = await CreateUserAsync(fixture, Email1);
            var bob = await CreateUserAsync(fixture, Email2);
            await fixture.UserManager.AddToRoleAsync(alice, "Admin");
            await fixture.UserManager.AddToRoleAsync(bob, "Admin");

            var result = await svc.SetAdminRoleAsync(alice.Id, false);

            Assert.Equal(SetAdminRoleResult.Success, result);
            Assert.False(await fixture.UserManager.IsInRoleAsync(alice, "Admin"));
        }
    }

    [Fact]
    public async Task SetAdminRole_blocks_removing_last_admin()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            var user = await CreateUserAsync(fixture, Email1);
            await fixture.UserManager.AddToRoleAsync(user, "Admin");

            var result = await svc.SetAdminRoleAsync(user.Id, false);

            Assert.Equal(SetAdminRoleResult.LastAdmin, result);
            Assert.True(await fixture.UserManager.IsInRoleAsync(user, "Admin"));
        }
    }

    [Fact]
    public async Task SetAdminRole_returns_NotFound_for_unknown_user()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            var result = await svc.SetAdminRoleAsync("nonexistent-id", true);
            Assert.Equal(SetAdminRoleResult.NotFound, result);
        }
    }

    [Fact]
    public async Task SetAdminRole_is_idempotent_for_existing_role()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            var user = await CreateUserAsync(fixture, Email1);
            await fixture.UserManager.AddToRoleAsync(user, "Admin");

            var result = await svc.SetAdminRoleAsync(user.Id, true);

            Assert.Equal(SetAdminRoleResult.Success, result);
            var roles = await fixture.UserManager.GetRolesAsync(user);
            Assert.Single(roles);
        }
    }
}
