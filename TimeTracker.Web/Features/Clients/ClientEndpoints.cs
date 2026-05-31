using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.Clients;

public static class ClientEndpoints
{
    public static void MapClientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients").RequireAuthorization();

        group.MapGet("/", async (IClientService service, bool includeArchived = false) =>
            Results.Ok(await service.GetAllClients(includeArchived)));

        group.MapGet("/{id:int}", async (int id, IClientService service) =>
        {
            var client = await service.GetClientById(id);
            return client is null ? Results.NotFound() : Results.Ok(client);
        });

        group.MapPost("/", async (ClientCreateRequest request, IClientService service) =>
        {
            await service.CreateClient(request);
            return Results.Created();
        });

        group.MapPut("/{id:int}", async (int id, ClientUpdateRequest request, IClientService service) =>
        {
            try { await service.UpdateClient(id, request); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        });

        group.MapPost("/{id:int}/archive", async (int id, IClientService service) =>
        {
            try { await service.ArchiveClient(id); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        });

        group.MapPost("/{id:int}/unarchive", async (int id, IClientService service) =>
        {
            try { await service.UnarchiveClient(id); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
        });

        group.MapDelete("/{id:int}", async (int id, IClientService service) =>
        {
            try { await service.DeleteClient(id); return Results.NoContent(); }
            catch (EntityNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
        });
    }
}
