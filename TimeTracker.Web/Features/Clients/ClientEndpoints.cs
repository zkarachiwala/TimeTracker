using Microsoft.AspNetCore.Authorization;
using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.Clients;

public static class ClientEndpoints
{
    public static void MapClientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients").RequireAuthorization();
        var adminGroup = app.MapGroup("/api/clients").RequireAuthorization(
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole("Admin").Build());

        group.MapGet("/", async (IClientService service, bool includeArchived, CancellationToken ct) =>
            Results.Ok(await service.GetAllClients(includeArchived, ct)));

        group.MapGet("/{id:int}", async (int id, IClientService service, CancellationToken ct) =>
        {
            var client = await service.GetClientById(id, ct);
            return client is null ? Results.NotFound() : Results.Ok(client);
        });

        adminGroup.MapPost("/", async (ClientCreateRequest request, IClientService service, CancellationToken ct) =>
        {
            await service.CreateClient(request, ct);
            return Results.Created();
        }).RequireRateLimiting("write");

        adminGroup.MapPut("/{id:int}", async (int id, ClientUpdateRequest request, IClientService service, CancellationToken ct) =>
        {
            try { await service.UpdateClient(id, request, ct); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        }).RequireRateLimiting("write");

        adminGroup.MapPost("/{id:int}/archive", async (int id, IClientService service, CancellationToken ct) =>
        {
            try { await service.ArchiveClient(id, ct); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
        }).RequireRateLimiting("write");

        adminGroup.MapPost("/{id:int}/unarchive", async (int id, IClientService service, CancellationToken ct) =>
        {
            try { await service.UnarchiveClient(id, ct); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        }).RequireRateLimiting("write");

        adminGroup.MapDelete("/{id:int}", async (int id, IClientService service, CancellationToken ct) =>
        {
            try { await service.DeleteClient(id, ct); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
        }).RequireRateLimiting("write");

        adminGroup.MapGet("/deleted", async (IClientService service, CancellationToken ct) =>
            Results.Ok(await service.GetDeletedClients(ct)));

        adminGroup.MapPost("/{id:int}/restore", async (int id, IClientService service, CancellationToken ct) =>
        {
            try { await service.RestoreClient(id, ct); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        }).RequireRateLimiting("write");
    }
}
