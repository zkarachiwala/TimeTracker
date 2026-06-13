using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.TimeEntries;

public static class TimeEntryEndpoints
{
    public static void MapTimeEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/timeentries").RequireAuthorization();

        group.MapGet("/active", async (ITimeEntryService svc, CancellationToken ct) =>
        {
            var entry = await svc.GetActiveTimeEntry(ct);
            return entry is null ? Results.NoContent() : Results.Ok(entry);
        });

        group.MapGet("/today", async (ITimeEntryService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetTodaysTimeEntries(ct)));

        group.MapGet("/year/{year:int}/all", async (int year, ITimeEntryService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAllTimeEntriesByYear(year, ct))).RequireRateLimiting("all-entries");

        group.MapGet("/{skip:int}/{limit:int}", async (int skip, int limit, ITimeEntryQueryService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetTimeEntries(skip, limit, ct)));

        group.MapGet("/{id:int}", async (int id, ITimeEntryService svc, CancellationToken ct) =>
        {
            var result = await svc.GetTimeEntryById(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/project/{projectId:int}/all", async (int projectId, ITimeEntryService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAllTimeEntriesByProject(projectId, ct))).RequireRateLimiting("all-entries");

        group.MapGet("/project/{projectId:int}/{skip:int}/{limit:int}", async (int projectId, int skip, int limit, ITimeEntryQueryService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetTimeEntriesByProjectId(projectId, skip, limit, ct)));

        group.MapGet("/year/{year:int}/{skip:int}/{limit:int}", async (int year, int skip, int limit, ITimeEntryQueryService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetTimeEntriesByYear(year, skip, limit, ct)));

        group.MapGet("/month/{month:int}/year/{year:int}/{skip:int}/{limit:int}", async (int month, int year, int skip, int limit, ITimeEntryQueryService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetTimeEntriesByMonth(month, year, skip, limit, ct)));

        group.MapGet("/day/{day:int}/month/{month:int}/year/{year:int}/{skip:int}/{limit:int}", async (int day, int month, int year, int skip, int limit, ITimeEntryQueryService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetTimeEntriesByDay(day, month, year, skip, limit, ct)));

        group.MapPost("/", async (TimeEntryCreateRequest request, ITimeEntryService svc, CancellationToken ct) =>
        {
            await svc.CreateTimeEntry(request, ct);
            return Results.Created();
        }).RequireRateLimiting("write");

        group.MapPut("/{id:int}", async (int id, TimeEntryUpdateRequest request, ITimeEntryService svc, CancellationToken ct) =>
        {
            try { await svc.UpdateTimeEntry(id, request, ct); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        }).RequireRateLimiting("write");

        group.MapDelete("/{id:int}", async (int id, ITimeEntryService svc, CancellationToken ct) =>
        {
            try { await svc.DeleteTimeEntry(id, ct); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        }).RequireRateLimiting("write");
    }
}
