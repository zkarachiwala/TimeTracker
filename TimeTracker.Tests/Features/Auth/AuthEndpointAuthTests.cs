using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Shared.Entities;
using TimeTracker.Web.Data;
using TimeTracker.Web.Features.Auth;
using Xunit;

namespace TimeTracker.Tests.Features.Auth;

public class AuthEndpointAuthTests
{
    private static IReadOnlyList<Endpoint> BuildEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddAuthorization();
        builder.Services.AddDbContext<IdentityDataContext>(o =>
            o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        builder.Services.AddIdentity<User, IdentityRole>()
            .AddEntityFrameworkStores<IdentityDataContext>();
        builder.Services.AddAuthentication();
        builder.Services.AddScoped<IExternalLoginService>(_ => new StubExternalLoginService());
        var app = builder.Build();
        app.MapAuthEndpoints();
        return ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints).ToList();
    }

    private static bool HasAdminRole(Endpoint endpoint) =>
        endpoint.Metadata
            .OfType<AuthorizationPolicy>()
            .Any(p => p.Requirements
                .OfType<RolesAuthorizationRequirement>()
                .Any(r => r.AllowedRoles.Contains("Admin")))
        || endpoint.Metadata
            .OfType<IAuthorizeData>()
            .Any(a => a.Roles?.Contains("Admin") == true);

    private static Endpoint? Find(IReadOnlyList<Endpoint> endpoints, string method, string pattern) =>
        endpoints.OfType<RouteEndpoint>()
            .FirstOrDefault(e =>
                e.RoutePattern.RawText == pattern &&
                e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(method) == true);

    [Fact]
    public void RevokeSessionsEndpoint_RequiresAdminRole()
    {
        var endpoints = BuildEndpoints();
        var endpoint = Find(endpoints, "POST", "/api/auth/revoke-sessions");
        Assert.NotNull(endpoint);
        Assert.True(HasAdminRole(endpoint),
            "POST /api/auth/revoke-sessions must require the Admin role but does not.");
    }

    private sealed class StubExternalLoginService : IExternalLoginService
    {
        public Task<ExternalLoginResult> FindOrCreateUserAsync(string email, string loginProvider, string providerKey) =>
            Task.FromResult(new ExternalLoginResult(ExternalLoginStatus.Success));
    }
}
