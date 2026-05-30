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
        params string[] allowedEmails)
    {
        var configEntries = allowedEmails
            .Select((email, i) => ($"Authentication:AllowedEmails:{i}", email))
            .ToArray();

        var fixture = new IdentityFixture(configEntries);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(allowedEmails
                .Select((e, i) => new KeyValuePair<string, string?>($"Authentication:AllowedEmails:{i}", e)))
            .Build();

        return (new ExternalLoginService(fixture.UserManager, config), fixture);
    }

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
}
