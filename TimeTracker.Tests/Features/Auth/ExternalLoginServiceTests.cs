using Microsoft.AspNetCore.Identity;
using TimeTracker.Shared.Entities;
using TimeTracker.Web.Features.Auth;
using TimeTracker.Tests.Infrastructure;
using Xunit;

namespace TimeTracker.Tests.Features.Auth;

[Collection("Services")]
public class ExternalLoginServiceTests
{
    private const string Email = "user@example.com";
    private const string Provider = "Google";
    private const string ProviderKey = "google-sub-123";

    private static ExternalLoginService CreateService(IdentityFixture fixture) =>
        new(fixture.UserManager);

    private static async Task<User> SeedUserAsync(IdentityFixture fixture, string email)
    {
        var user = new User { UserName = email, Email = email, EmailConfirmed = true };
        await fixture.UserManager.CreateAsync(user);
        return user;
    }

    [Fact]
    public async Task Returns_EmailNotAllowed_when_no_user_record_exists()
    {
        using var fixture = new IdentityFixture();
        var result = await CreateService(fixture).FindOrCreateUserAsync(Email, Provider, ProviderKey);
        Assert.Equal(ExternalLoginStatus.EmailNotAllowed, result.Status);
    }

    [Fact]
    public async Task Returns_Success_when_user_record_exists()
    {
        using var fixture = new IdentityFixture();
        await SeedUserAsync(fixture, Email);
        var result = await CreateService(fixture).FindOrCreateUserAsync(Email, Provider, ProviderKey);
        Assert.Equal(ExternalLoginStatus.Success, result.Status);
    }

    [Fact]
    public async Task Returns_correct_user_on_success()
    {
        using var fixture = new IdentityFixture();
        var seeded = await SeedUserAsync(fixture, Email);
        var result = await CreateService(fixture).FindOrCreateUserAsync(Email, Provider, ProviderKey);
        Assert.Equal(seeded.Id, result.User!.Id);
    }

    [Fact]
    public async Task Email_check_is_case_insensitive()
    {
        using var fixture = new IdentityFixture();
        await SeedUserAsync(fixture, Email);
        var result = await CreateService(fixture).FindOrCreateUserAsync(Email.ToUpper(), Provider, ProviderKey);
        Assert.Equal(ExternalLoginStatus.Success, result.Status);
    }

    [Fact]
    public async Task Links_external_provider_on_first_login()
    {
        using var fixture = new IdentityFixture();
        await SeedUserAsync(fixture, Email);

        await CreateService(fixture).FindOrCreateUserAsync(Email, Provider, ProviderKey);

        var user = await fixture.UserManager.FindByEmailAsync(Email);
        var logins = await fixture.UserManager.GetLoginsAsync(user!);
        Assert.Single(logins);
        Assert.Equal(Provider, logins[0].LoginProvider);
        Assert.Equal(ProviderKey, logins[0].ProviderKey);
    }

    [Fact]
    public async Task Does_not_duplicate_provider_link_on_subsequent_login()
    {
        using var fixture = new IdentityFixture();
        await SeedUserAsync(fixture, Email);
        var svc = CreateService(fixture);

        await svc.FindOrCreateUserAsync(Email, Provider, ProviderKey);
        await svc.FindOrCreateUserAsync(Email, Provider, ProviderKey);

        var user = await fixture.UserManager.FindByEmailAsync(Email);
        var logins = await fixture.UserManager.GetLoginsAsync(user!);
        Assert.Single(logins);
    }

    [Fact]
    public async Task Links_second_provider_independently()
    {
        using var fixture = new IdentityFixture();
        await SeedUserAsync(fixture, Email);
        var svc = CreateService(fixture);

        await svc.FindOrCreateUserAsync(Email, Provider, ProviderKey);
        await svc.FindOrCreateUserAsync(Email, "Entra", "entra-oid-456");

        var user = await fixture.UserManager.FindByEmailAsync(Email);
        var logins = await fixture.UserManager.GetLoginsAsync(user!);
        Assert.Equal(2, logins.Count);
    }
}
