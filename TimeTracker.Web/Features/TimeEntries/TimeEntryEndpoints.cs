using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.TimeEntries;

public static class TimeEntryEndpoints
{
    public static void MapTimeEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/timeentries").RequireAuthorization();

        group.MapGet("/active", async (ITimeEntryService svc) =>
        {
            var entry = await svc.GetActiveTimeEntry();
            return entry is null ? Results.NoContent() : Results.Ok(entry);
        });

        group.MapGet("/today", async (ITimeEntryService svc) =>
            Results.Ok(await svc.GetTodaysTimeEntries()));

        group.MapGet("/year/{year:int}/all", async (int year, ITimeEntryService svc) =>
            Results.Ok(await svc.GetAllTimeEntriesByYear(year)));

        group.MapGet("/{skip:int}/{limit:int}", async (int skip, int limit, ITimeEntryService svc) =>
            Results.Ok(await svc.GetTimeEntries(skip, limit)));

        group.MapGet("/{id:int}", async (int id, ITimeEntryService svc) =>
        {
            var result = await svc.GetTimeEntryById(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/project/{projectId:int}/all", async (int projectId, ITimeEntryService svc) =>
            Results.Ok(await svc.GetAllTimeEntriesByProject(projectId)));

        group.MapGet("/project/{projectId:int}/{skip:int}/{limit:int}", async (int projectId, int skip, int limit, ITimeEntryService svc) =>
            Results.Ok(await svc.GetTimeEntriesByProjectId(projectId, skip, limit)));

        group.MapGet("/year/{year:int}/{skip:int}/{limit:int}", async (int year, int skip, int limit, ITimeEntryService svc) =>
            Results.Ok(await svc.GetTimeEntriesByYear(year, skip, limit)));

        group.MapGet("/month/{month:int}/year/{year:int}/{skip:int}/{limit:int}", async (int month, int year, int skip, int limit, ITimeEntryService svc) =>
            Results.Ok(await svc.GetTimeEntriesByMonth(month, year, skip, limit)));

        group.MapGet("/day/{day:int}/month/{month:int}/year/{year:int}/{skip:int}/{limit:int}", async (int day, int month, int year, int skip, int limit, ITimeEntryService svc) =>
            Results.Ok(await svc.GetTimeEntriesByDay(day, month, year, skip, limit)));

        group.MapPost("/", async (TimeEntryCreateRequest request, ITimeEntryService svc) =>
        {
            await svc.CreateTimeEntry(request);
            return Results.Created();
        });

        group.MapPut("/{id:int}", async (int id, TimeEntryUpdateRequest request, ITimeEntryService svc) =>
        {
            try { await svc.UpdateTimeEntry(id, request); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        });

        group.MapDelete("/{id:int}", async (int id, ITimeEntryService svc) =>
        {
            try { await svc.DeleteTimeEntry(id); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        });
    }
}
