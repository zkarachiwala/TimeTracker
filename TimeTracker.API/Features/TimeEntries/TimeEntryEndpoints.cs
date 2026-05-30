using Microsoft.AspNetCore.Mvc;

namespace TimeTracker.API.Features.TimeEntries;

public static class TimeEntryEndpoints
{
    public static void MapTimeEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/timeentries").RequireAuthorization();

        group.MapGet("/{skip:int}/{limit:int}", async (int skip, int limit, ITimeEntryService svc) =>
            Results.Ok(await svc.GetTimeEntries(skip, limit)));

        group.MapGet("/{id:int}", async (int id, ITimeEntryService svc) =>
        {
            var result = await svc.GetTimeEntryById(id);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

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
            await svc.UpdateTimeEntry(id, request);
            return Results.NoContent();
        });

        group.MapDelete("/{id:int}", async (int id, ITimeEntryService svc) =>
        {
            await svc.DeleteTimeEntry(id);
            return Results.NoContent();
        });
    }
}
