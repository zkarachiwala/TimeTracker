using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Shared.Exceptions;
using TimeTracker.Web.Features.Projects;
using Xunit;

namespace TimeTracker.Tests.Features.Projects;

public class ProjectEndpointAuthTests
{
    private static IReadOnlyList<Endpoint> BuildEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<IProjectService>(_ => new StubProjectService());
        var app = builder.Build();
        app.MapProjectEndpoints();
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
    [InlineData("POST",   "/api/projects/")]
    [InlineData("PUT",    "/api/projects/{id:int}")]
    [InlineData("DELETE", "/api/projects/{id:int}")]
    public void MutationEndpoints_RequireAdminRole(string method, string pattern)
    {
        var endpoints = BuildEndpoints();
        var endpoint = Find(endpoints, method, pattern);
        Assert.NotNull(endpoint);
        Assert.True(HasAdminRole(endpoint),
            $"{method} {pattern} must require the Admin role but does not.");
    }

    [Theory]
    [InlineData("GET", "/api/projects/")]
    [InlineData("GET", "/api/projects/{id:int}")]
    public void ReadEndpoints_DoNotRequireAdminRole(string method, string pattern)
    {
        var endpoints = BuildEndpoints();
        var endpoint = Find(endpoints, method, pattern);
        Assert.NotNull(endpoint);
        Assert.False(HasAdminRole(endpoint),
            $"{method} {pattern} should not require Admin role.");
    }

    private sealed class StubProjectService : IProjectService
    {
        public Task<List<ProjectResponse>> GetAllProjects(CancellationToken ct = default) => Task.FromResult(new List<ProjectResponse>());
        public Task<List<ProjectResponse>> GetAssignedProjects(CancellationToken ct = default) => Task.FromResult(new List<ProjectResponse>());
        public Task<ProjectResponse?> GetProjectById(int id, CancellationToken ct = default) => Task.FromResult<ProjectResponse?>(null);
        public Task CreateProject(ProjectCreateRequest request, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateProject(int id, ProjectUpdateRequest request, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteProject(int id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<DeletedProjectResponse>> GetDeletedProjects(CancellationToken ct = default) => Task.FromResult(new List<DeletedProjectResponse>());
        public Task RestoreProject(int id, CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<ProjectUserResponse>> GetProjectUsers(int projectId, CancellationToken ct = default) => Task.FromResult(new List<ProjectUserResponse>());
        public Task AssignUserToProject(int projectId, string userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task UnassignUserFromProject(int projectId, string userId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
