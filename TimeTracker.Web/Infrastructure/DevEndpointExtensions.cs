using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using TimeTracker.Web.Data;
using TimeTracker.Web.Dev;
using TimeTracker.Shared.Entities;

namespace TimeTracker.Web.Infrastructure;

public static class DevEndpointExtensions
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        // Triggers the DatabaseWarmupMiddleware to preview the "waking up" page
        app.MapGet("/dev/db-wakeup-demo", _ =>
            throw CreateFakeConnectivityException());

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

    private static SqlException CreateFakeConnectivityException()
    {
        var errorCtor = typeof(SqlError).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            [typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string),
             typeof(string), typeof(int), typeof(int), typeof(Exception)])!;

        var errors = (SqlErrorCollection)typeof(SqlErrorCollection)
            .GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                [])!
            .Invoke(null)!;

        var error = errorCtor.Invoke(
            [4060, (byte)0, (byte)0, "localhost", "Cannot open database \"TimeTrackerDb\". It may be waking from auto-pause.", "", 0, 0, null]);

        typeof(SqlErrorCollection).GetMethod("Add",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(errors, [error]);

        var sqlExCtor = typeof(SqlException).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            [typeof(string), typeof(SqlErrorCollection), typeof(Exception), typeof(Guid)])!;

        return (SqlException)sqlExCtor.Invoke(["Database is waking from idle.", errors, null, Guid.NewGuid()]);
    }
}
