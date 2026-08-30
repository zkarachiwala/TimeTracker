using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
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

        // Signs in the first Admin user — dev/CI only. This endpoint's only guard used to be that
        // MapDevEndpoints is called inside `if (app.Environment.IsDevelopment())`; anyone reaching a
        // dev/CI instance, or any environment where ASPNETCORE_ENVIRONMENT is misconfigured, got a
        // full admin session for free. The token below is defense-in-depth against that
        // misconfiguration case — it does not replace the environment check. See #341.
        //
        // Fails closed: if DevTools:LoginToken isn't configured at all, the endpoint is unreachable.
        app.MapGet("/api/dev/login", async (
            HttpRequest request,
            IConfiguration configuration,
            UserManager<User> userManager,
            SignInManager<User> signInManager) =>
        {
            var expectedToken = configuration["DevTools:LoginToken"];
            var providedToken = request.Headers[LoginTokenHeader].ToString();
            if (string.IsNullOrEmpty(expectedToken) || !IsValidToken(providedToken, expectedToken))
                return Results.NotFound();

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

    public const string LoginTokenHeader = "X-Dev-Login-Token";

    // Fixed-time comparison — this guards a real admin session, so it shouldn't leak match-length
    // via a timing side channel even though the token itself isn't a long-lived secret. Public
    // (not private) so it can be unit-tested directly without standing up the full endpoint pipeline.
    public static bool IsValidToken(string providedToken, string expectedToken)
    {
        if (providedToken.Length == 0) return false;

        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);
        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
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
