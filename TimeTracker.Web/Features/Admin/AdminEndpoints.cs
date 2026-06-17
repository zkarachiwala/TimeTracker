using Microsoft.AspNetCore.Authorization;
using TimeTracker.Contracts.Features.Admin;
using TimeTracker.Shared.Exceptions;

namespace TimeTracker.Web.Features.Admin;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization(
            new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole("Admin").Build());

        group.MapGet("/users", async (IUserManagementService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetUsersAsync(ct)));

        group.MapPost("/users", async (AddUserRequest request, IUserManagementService svc, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest("Email is required.");
            try
            {
                await svc.AddUserAsync(request.Email, ct);
                return Results.Created();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).RequireRateLimiting("write");

        group.MapPut("/users/{userId}/role", async (string userId, SetAdminRoleRequest request, IUserManagementService svc, CancellationToken ct) =>
        {
            try
            {
                await svc.SetAdminRoleAsync(userId, request.IsAdmin, ct);
                return Results.NoContent();
            }
            catch (EntityNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(ex.Message);
            }
        }).RequireRateLimiting("write");
    }
}
