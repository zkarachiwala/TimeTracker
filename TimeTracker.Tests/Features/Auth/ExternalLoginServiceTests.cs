using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using TimeTracker.Web.Features.Auth;
using TimeTracker.Tests.Infrastructure;
using Xunit;

namespace TimeTracker.Tests.Features.Auth;

[Collection("Services")]
public class ExternalLoginServiceTests
{
    private const string AllowedEmail = "allowed@example.com";
    private const string Provider = "Google";
    private const string ProviderKey = "google-sub-123";

    private static ExternalLoginService CreateService(IdentityFixture fixture) =>
        new(fixture.UserManager, fixture.UserManager.GetType().Assembly
            .GetTypes() // just need IConfiguration — grab it from fixture via the service
            .Select(_ => (IConfiguration?)null).First()!);

    // Helper that wires everything from the fixture
    private static (ExternalLoginService Service, IdentityFixture Fixture) Build(
        string[] allowedEmails,
        string? adminEmail = null)
    {
        var configPairs = allowedEmails
            .Select((e, i) => new KeyValuePair<string, string?>($"Authentication:AllowedEmails:{i}", e))
            .ToList();

        if (adminEmail is not null)
            configPairs.Add(new KeyValuePair<string, string?>("Authentication:AdminEmail", adminEmail));

        var fixture = new IdentityFixture(allowedEmails
            .Select((e, i) => ($"Authentication:AllowedEmails:{i}", e)).ToArray());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configPairs)
            .Build();

        return (new ExternalLoginService(fixture.UserManager, config), fixture);
    }

    private static (ExternalLoginService Service, IdentityFixture Fixture) Build(
        params string[] allowedEmails) => Build(allowedEmails, adminEmail: null);

    [Fact]
    public async Task Returns_EmailNotAllowed_when_email_not_in_config()
    {
        var (svc, fixture) = Build(AllowedEmail);
        using (fixture)
        {
            var result = await svc.FindOrCreateUserAsync("other@example.com", Provider, ProviderKey);
            Assert.Equal(ExternalLoginStatus.EmailNotAllowed, result.Status);
        }
    }

    [Fact]
    public async Task Returns_EmailNotAllowed_when_allowed_list_is_empty()
    {
        var (svc, fixture) = Build();
        using (fixture)
        {
            var result = await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);
            Assert.Equal(ExternalLoginStatus.EmailNotAllowed, result.Status);
        }
    }

    [Fact]
    public async Task Allowed_email_check_is_case_insensitive()
    {
        var (svc, fixture) = Build(AllowedEmail);
        using (fixture)
        {
            var result = await svc.FindOrCreateUserAsync(AllowedEmail.ToUpper(), Provider, ProviderKey);
            Assert.Equal(ExternalLoginStatus.Success, result.Status);
        }
    }

    [Fact]
    public async Task Creates_new_user_on_first_login()
    {
        var (svc, fixture) = Build(AllowedEmail);
        using (fixture)
        {
            var result = await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);

            Assert.Equal(ExternalLoginStatus.Success, result.Status);
            Assert.NotNull(result.User);
            Assert.Equal(AllowedEmail, result.User.Email);

            var stored = await fixture.UserManager.FindByEmailAsync(AllowedEmail);
            Assert.NotNull(stored);
        }
    }

    [Fact]
    public async Task Returns_existing_user_on_subsequent_login()
    {
        var (svc, fixture) = Build(AllowedEmail);
        using (fixture)
        {
            var first = await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);
            var second = await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);

            Assert.Equal(ExternalLoginStatus.Success, second.Status);
            Assert.Equal(first.User!.Id, second.User!.Id);

            var allUsers = fixture.UserManager.Users.ToList();
            Assert.Single(allUsers);
        }
    }

    [Fact]
    public async Task Links_external_provider_on_first_login()
    {
        var (svc, fixture) = Build(AllowedEmail);
        using (fixture)
        {
            await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);

            var user = await fixture.UserManager.FindByEmailAsync(AllowedEmail);
            var logins = await fixture.UserManager.GetLoginsAsync(user!);

            Assert.Single(logins);
            Assert.Equal(Provider, logins[0].LoginProvider);
            Assert.Equal(ProviderKey, logins[0].ProviderKey);
        }
    }

    [Fact]
    public async Task Does_not_duplicate_provider_link_on_subsequent_login()
    {
        var (svc, fixture) = Build(AllowedEmail);
        using (fixture)
        {
            await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);
            await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);

            var user = await fixture.UserManager.FindByEmailAsync(AllowedEmail);
            var logins = await fixture.UserManager.GetLoginsAsync(user!);

            Assert.Single(logins);
        }
    }

    [Fact]
    public async Task Links_second_provider_independently()
    {
        var (svc, fixture) = Build(AllowedEmail);
        using (fixture)
        {
            await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);
            await svc.FindOrCreateUserAsync(AllowedEmail, "Entra", "entra-oid-456");

            var user = await fixture.UserManager.FindByEmailAsync(AllowedEmail);
            var logins = await fixture.UserManager.GetLoginsAsync(user!);

            Assert.Equal(2, logins.Count);
            Assert.Contains(logins, l => l.LoginProvider == "Google");
            Assert.Contains(logins, l => l.LoginProvider == "Entra");
        }
    }

    [Fact]
    public async Task Assigns_Admin_role_when_email_matches_AdminEmail()
    {
        var (svc, fixture) = Build([AllowedEmail], adminEmail: AllowedEmail);
        using (fixture)
        {
            await fixture.RoleManager.CreateAsync(new IdentityRole("Admin"));

            await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);

            var user = await fixture.UserManager.FindByEmailAsync(AllowedEmail);
            var isAdmin = await fixture.UserManager.IsInRoleAsync(user!, "Admin");
            Assert.True(isAdmin);
        }
    }

    [Fact]
    public async Task Does_not_assign_Admin_role_when_email_does_not_match_AdminEmail()
    {
        var otherEmail = "other@example.com";
        var (svc, fixture) = Build([AllowedEmail, otherEmail], adminEmail: AllowedEmail);
        using (fixture)
        {
            await fixture.RoleManager.CreateAsync(new IdentityRole("Admin"));

            await svc.FindOrCreateUserAsync(otherEmail, Provider, ProviderKey);

            var user = await fixture.UserManager.FindByEmailAsync(otherEmail);
            var isAdmin = await fixture.UserManager.IsInRoleAsync(user!, "Admin");
            Assert.False(isAdmin);
        }
    }

    [Fact]
    public async Task Admin_role_assignment_is_case_insensitive()
    {
        var (svc, fixture) = Build([AllowedEmail], adminEmail: AllowedEmail.ToUpper());
        using (fixture)
        {
            await fixture.RoleManager.CreateAsync(new IdentityRole("Admin"));

            await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);

            var user = await fixture.UserManager.FindByEmailAsync(AllowedEmail);
            var isAdmin = await fixture.UserManager.IsInRoleAsync(user!, "Admin");
            Assert.True(isAdmin);
        }
    }

    [Fact]
    public async Task Admin_role_assignment_is_idempotent()
    {
        var (svc, fixture) = Build([AllowedEmail], adminEmail: AllowedEmail);
        using (fixture)
        {
            await fixture.RoleManager.CreateAsync(new IdentityRole("Admin"));

            await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);
            await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);

            var user = await fixture.UserManager.FindByEmailAsync(AllowedEmail);
            var roles = await fixture.UserManager.GetRolesAsync(user!);
            Assert.Single(roles);
        }
    }

    [Fact]
    public async Task Does_not_assign_Admin_role_when_AdminEmail_not_configured()
    {
        var (svc, fixture) = Build([AllowedEmail]);
        using (fixture)
        {
            await fixture.RoleManager.CreateAsync(new IdentityRole("Admin"));

            await svc.FindOrCreateUserAsync(AllowedEmail, Provider, ProviderKey);

            var user = await fixture.UserManager.FindByEmailAsync(AllowedEmail);
            var isAdmin = await fixture.UserManager.IsInRoleAsync(user!, "Admin");
            Assert.False(isAdmin);
        }
    }
}
