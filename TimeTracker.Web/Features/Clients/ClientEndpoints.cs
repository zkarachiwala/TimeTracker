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

        group.MapGet("/", async (IClientService service, bool includeArchived = false) =>
            Results.Ok(await service.GetAllClients(includeArchived)));

        group.MapGet("/{id:int}", async (int id, IClientService service) =>
        {
            var client = await service.GetClientById(id);
            return client is null ? Results.NotFound() : Results.Ok(client);
        });

        adminGroup.MapPost("/", async (ClientCreateRequest request, IClientService service) =>
        {
            await service.CreateClient(request);
            return Results.Created();
        });

        adminGroup.MapPut("/{id:int}", async (int id, ClientUpdateRequest request, IClientService service) =>
        {
            try { await service.UpdateClient(id, request); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        });

        adminGroup.MapPost("/{id:int}/archive", async (int id, IClientService service) =>
        {
            try { await service.ArchiveClient(id); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        });

        adminGroup.MapPost("/{id:int}/unarchive", async (int id, IClientService service) =>
        {
            try { await service.UnarchiveClient(id); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        });

        adminGroup.MapDelete("/{id:int}", async (int id, IClientService service) =>
        {
            try { await service.DeleteClient(id); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
        });
    }
}
