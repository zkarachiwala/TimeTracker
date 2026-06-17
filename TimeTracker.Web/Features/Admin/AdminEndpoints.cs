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
