using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Web.Features.Clients;
using Xunit;

namespace TimeTracker.Tests.Features.Clients;

public class ClientEndpointAuthTests
{
    private static IReadOnlyList<Endpoint> BuildEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<IClientService>(_ => new StubClientService());
        var app = builder.Build();
        app.MapClientEndpoints();
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

    [Theory]
    [InlineData("POST",   "/api/clients/")]
    [InlineData("PUT",    "/api/clients/{id:int}")]
    [InlineData("DELETE", "/api/clients/{id:int}")]
    [InlineData("POST",   "/api/clients/{id:int}/archive")]
    [InlineData("POST",   "/api/clients/{id:int}/unarchive")]
    public void MutationEndpoints_RequireAdminRole(string method, string pattern)
    {
        var endpoints = BuildEndpoints();
        var endpoint = Find(endpoints, method, pattern);
        Assert.NotNull(endpoint);
        Assert.True(HasAdminRole(endpoint),
            $"{method} {pattern} must require the Admin role but does not.");
    }

    [Theory]
    [InlineData("GET", "/api/clients/")]
    [InlineData("GET", "/api/clients/{id:int}")]
    public void ReadEndpoints_DoNotRequireAdminRole(string method, string pattern)
    {
        var endpoints = BuildEndpoints();
        var endpoint = Find(endpoints, method, pattern);
        Assert.NotNull(endpoint);
        Assert.False(HasAdminRole(endpoint),
            $"{method} {pattern} should not require Admin role.");
    }

    private sealed class StubClientService : IClientService
    {
        public Task<List<ClientResponse>> GetAllClients(bool includeArchived = false, CancellationToken ct = default) => Task.FromResult(new List<ClientResponse>());
        public Task<ClientResponse?> GetClientById(int id, CancellationToken ct = default) => Task.FromResult<ClientResponse?>(null);
        public Task CreateClient(ClientCreateRequest request, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateClient(int id, ClientUpdateRequest request, CancellationToken ct = default) => Task.CompletedTask;
        public Task ArchiveClient(int id, CancellationToken ct = default) => Task.CompletedTask;
        public Task UnarchiveClient(int id, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteClient(int id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<DeletedClientResponse>> GetDeletedClients(CancellationToken ct = default) => Task.FromResult(new List<DeletedClientResponse>());
        public Task RestoreClient(int id, CancellationToken ct = default) => Task.CompletedTask;
    }
}
