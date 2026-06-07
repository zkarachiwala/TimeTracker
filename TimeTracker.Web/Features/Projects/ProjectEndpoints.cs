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

        group.MapGet("/", async (IProjectService svc) =>
            Results.Ok(await svc.GetAllProjects()));

        group.MapGet("/{id:int}", async (int id, IProjectService svc) =>
        {
            var result = await svc.GetProjectById(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        adminGroup.MapPost("/", async (ProjectCreateRequest request, IProjectService svc) =>
        {
            await svc.CreateProject(request);
            return Results.Created();
        });

        adminGroup.MapPut("/{id:int}", async (int id, ProjectUpdateRequest request, IProjectService svc) =>
        {
            try { await svc.UpdateProject(id, request); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        });

        adminGroup.MapDelete("/{id:int}", async (int id, IProjectService svc) =>
        {
            try { await svc.DeleteProject(id); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        });
    }
}
