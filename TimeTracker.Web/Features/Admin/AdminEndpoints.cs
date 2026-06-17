using Microsoft.AspNetCore.Authorization;
using TimeTracker.Contracts.Features.Admin;

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

            var result = await svc.AddUserAsync(request.Email, ct);
            return result switch
            {
                AddUserResult.Success     => Results.Created(),
                AddUserResult.AlreadyExists => Results.Conflict("A user with that email already exists."),
                _                         => Results.Problem("Failed to create user.")
            };
        }).RequireRateLimiting("write");

        group.MapPut("/users/{userId}/role", async (string userId, SetAdminRoleRequest request, IUserManagementService svc, CancellationToken ct) =>
        {
            var result = await svc.SetAdminRoleAsync(userId, request.IsAdmin, ct);
            return result switch
            {
                SetAdminRoleResult.Success   => Results.NoContent(),
                SetAdminRoleResult.LastAdmin => Results.Conflict("Cannot remove the last admin."),
                _                            => Results.NotFound()
            };
        }).RequireRateLimiting("write");
    }
}
