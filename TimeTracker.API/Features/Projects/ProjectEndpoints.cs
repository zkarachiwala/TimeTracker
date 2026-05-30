namespace TimeTracker.API.Features.Projects;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireAuthorization();

        group.MapGet("/", async (IProjectService svc) =>
            Results.Ok(await svc.GetAllProjects()));

        group.MapGet("/{id:int}", async (int id, IProjectService svc) =>
        {
            var result = await svc.GetProjectById(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/", async (ProjectCreateRequest request, IProjectService svc) =>
        {
            await svc.CreateProject(request);
            return Results.Created();
        });

        group.MapPut("/{id:int}", async (int id, ProjectUpdateRequest request, IProjectService svc) =>
        {
            await svc.UpdateProject(id, request);
            return Results.NoContent();
        });

        group.MapDelete("/{id:int}", async (int id, IProjectService svc) =>
        {
            await svc.DeleteProject(id);
            return Results.NoContent();
        });
    }
}
