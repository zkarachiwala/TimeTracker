using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TimeTracker.Web.Data;
using TimeTracker.Web.Dev;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Infrastructure;

public static class DevEndpointExtensions
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        // Signs in the first Admin user — dev/CI only, never deployed to production
        app.MapGet("/api/dev/login", async (
            UserManager<User> userManager,
            SignInManager<User> signInManager) =>
        {
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            var user = admins.FirstOrDefault();
            if (user is null)
                return Results.Problem("No admin user found. Run /api/dev/seed first.");
            await signInManager.SignInAsync(user, isPersistent: true);
            return Results.Content($"<html><body>Signed in as {user.Email}</body></html>", "text/html");
        });

        var adminPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole("Admin")
            .Build();

        app.MapPost("/api/dev/seed", async (
            IDbContextFactory<TimeTrackerDataContext> ctxFactory,
            UserManager<User> userManager) =>
        {
            var result = await DevDataSeeder.SeedAsync(ctxFactory, userManager);
            return Results.Ok(result);
        }).RequireAuthorization(adminPolicy);

        app.MapPost("/api/dev/clear", async (
            IDbContextFactory<TimeTrackerDataContext> ctxFactory) =>
        {
            await using var ctx = await ctxFactory.CreateDbContextAsync();
            ctx.TimeEntries.RemoveRange(ctx.TimeEntries);
            ctx.ProjectUsers.RemoveRange(ctx.ProjectUsers);
            ctx.Projects.RemoveRange(ctx.Projects);
            ctx.Clients.RemoveRange(ctx.Clients);
            await ctx.SaveChangesAsync();
            return Results.Ok("Cleared all time entries, projects and clients.");
        }).RequireAuthorization(adminPolicy);

        return app;
    }
}
