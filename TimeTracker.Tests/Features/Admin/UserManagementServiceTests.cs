using Microsoft.AspNetCore.Identity;
using TimeTracker.Shared.Entities;
using TimeTracker.Shared.Exceptions;
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
    public async Task AddUser_creates_user_record()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            await svc.AddUserAsync(Email1);
            Assert.NotNull(await fixture.UserManager.FindByEmailAsync(Email1));
        }
    }

    [Fact]
    public async Task AddUser_throws_for_duplicate_email()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            await svc.AddUserAsync(Email1);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddUserAsync(Email1));
        }
    }

    [Fact]
    public async Task AddUser_is_case_insensitive_for_duplicate_check()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            await svc.AddUserAsync(Email1);
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddUserAsync(Email1.ToUpper()));
        }
    }

    [Fact]
    public async Task AddUser_new_user_has_no_roles()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            await svc.AddUserAsync(Email1);
            var user = await fixture.UserManager.FindByEmailAsync(Email1);
            var roles = await fixture.UserManager.GetRolesAsync(user!);
            Assert.Empty(roles);
        }
    }

    [Fact]
    public async Task SetAdminRole_promotes_user_to_admin()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            var user = await CreateUserAsync(fixture, Email1);
            await svc.SetAdminRoleAsync(user.Id, true);
            Assert.True(await fixture.UserManager.IsInRoleAsync(user, "Admin"));
        }
    }

    [Fact]
    public async Task SetAdminRole_demotes_user_when_another_admin_exists()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            var alice = await CreateUserAsync(fixture, Email1);
            var bob = await CreateUserAsync(fixture, Email2);
            await fixture.UserManager.AddToRoleAsync(alice, "Admin");
            await fixture.UserManager.AddToRoleAsync(bob, "Admin");

            await svc.SetAdminRoleAsync(alice.Id, false);

            Assert.False(await fixture.UserManager.IsInRoleAsync(alice, "Admin"));
        }
    }

    [Fact]
    public async Task SetAdminRole_throws_when_removing_last_admin()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            var user = await CreateUserAsync(fixture, Email1);
            await fixture.UserManager.AddToRoleAsync(user, "Admin");

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SetAdminRoleAsync(user.Id, false));

            Assert.True(await fixture.UserManager.IsInRoleAsync(user, "Admin"));
        }
    }

    [Fact]
    public async Task SetAdminRole_throws_for_unknown_user()
    {
        var (svc, fixture) = await BuildAsync();
        using (fixture)
        {
            await Assert.ThrowsAsync<EntityNotFoundException>(() => svc.SetAdminRoleAsync("nonexistent-id", true));
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

            await svc.SetAdminRoleAsync(user.Id, true);

            var roles = await fixture.UserManager.GetRolesAsync(user);
            Assert.Single(roles);
        }
    }
}
