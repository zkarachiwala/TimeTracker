using Microsoft.AspNetCore.Authorization;
using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.Projects;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireAuthorization();
        var adminGroup = app.MapGroup("/api/projects").RequireAuthorization(
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole("Admin").Build());

        group.MapGet("/", async (IProjectService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAllProjects(ct)));

        group.MapGet("/{id:int}", async (int id, IProjectService svc, CancellationToken ct) =>
        {
            var result = await svc.GetProjectById(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        adminGroup.MapPost("/", async (ProjectCreateRequest request, IProjectService svc, CancellationToken ct) =>
        {
            await svc.CreateProject(request, ct);
            return Results.Created();
        }).RequireRateLimiting("write");

        adminGroup.MapPut("/{id:int}", async (int id, ProjectUpdateRequest request, IProjectService svc, CancellationToken ct) =>
        {
            try { await svc.UpdateProject(id, request, ct); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        }).RequireRateLimiting("write");

        adminGroup.MapDelete("/{id:int}", async (int id, IProjectService svc, CancellationToken ct) =>
        {
            try { await svc.DeleteProject(id, ct); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        }).RequireRateLimiting("write");

        adminGroup.MapGet("/deleted", async (IProjectService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetDeletedProjects(ct)));

        adminGroup.MapPost("/{id:int}/restore", async (int id, IProjectService svc, CancellationToken ct) =>
        {
            try { await svc.RestoreProject(id, ct); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        }).RequireRateLimiting("write");

        group.MapGet("/{id:int}/users", async (int id, IProjectService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetProjectUsers(id, ct)));

        adminGroup.MapPost("/{id:int}/users", async (int id, AssignUserRequest request, IProjectService svc, CancellationToken ct) =>
        {
            try { await svc.AssignUserToProject(id, request.UserId, ct); return Results.Created(); }
            catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
        }).RequireRateLimiting("write");

        adminGroup.MapDelete("/{id:int}/users/{userId}", async (int id, string userId, IProjectService svc, CancellationToken ct) =>
        {
            try { await svc.UnassignUserFromProject(id, userId, ct); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        }).RequireRateLimiting("write");
    }
}
