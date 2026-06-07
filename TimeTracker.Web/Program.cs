using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MudBlazor.Services;
using TimeTracker.Web;
using Mapster;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Scalar.AspNetCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Dev;
using TimeTracker.Web.Features.Auth;
using TimeTracker.Web.Features.Clients;
using TimeTracker.Web.Features.Projects;
using TimeTracker.Web.Features.TimeEntries;
using TimeTracker.Web.Infrastructure;
using TimeTracker.Web.Shared;
using TimeTracker.Shared.Entities;

var builder = WebApplication.CreateBuilder(args);

var timeTrackerConnection = GetConnectionString(builder, "TimeTrackerConnection", "DbUser", "DbPassword");
var identityConnection = GetConnectionString(builder, "IdentityConnection", "DbUser", "DbPassword");

builder.Services.AddMudServices();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddDbContextFactory<TimeTrackerDataContext>(o => o.UseSqlServer(timeTrackerConnection));
builder.Services.AddDbContext<IdentityDataContext>(o => o.UseSqlServer(identityConnection));

builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<IdentityDataContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/login";
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", policy =>
    {
        policy.PermitLimit = 10;
        policy.Window = TimeSpan.FromMinutes(1);
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHttpContextAccessor();

// Provide HttpClient for SSR prerender of WASM components that inject it.
// The client is never called server-side — IsBrowser() guards in WASM components
// prevent actual HTTP calls during prerender; this satisfies the DI requirement.
builder.Services.AddScoped(sp =>
{
    var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
    var baseUri = ctx is not null
        ? new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host}/")
        : new Uri("https://localhost:7006/");
    return new HttpClient { BaseAddress = baseUri };
});

builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IExternalLoginService, ExternalLoginService>();

var app = builder.Build();

TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

using (var scope = app.Services.CreateScope())
{
    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TimeTrackerDataContext>>();
    await using var appCtx = await ctxFactory.CreateDbContextAsync();
    await appCtx.Database.MigrateAsync();

    var identityCtx = scope.ServiceProvider.GetRequiredService<IdentityDataContext>();
    await identityCtx.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Signs in the first Admin user — dev/CI only, never deployed to production
    app.MapGet("/api/dev/login", async (
        UserManager<User> userManager,
        SignInManager<User> signInManager) =>
    {
        var adminRole = "Admin";
        var admins = await userManager.GetUsersInRoleAsync(adminRole);
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
}

app.UseHsts();
app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TimeTracker.Client.Features.Timer.Pages.TimerPage).Assembly);

app.MapControllers();
app.MapTimeEntryEndpoints();
app.MapProjectEndpoints();
app.MapClientEndpoints();
app.MapAuthEndpoints();

var allowedEmails = app.Configuration.GetSection("Authentication:AllowedEmails").Get<string[]>();
if (allowedEmails is null || allowedEmails.Length == 0)
    throw new InvalidOperationException("Authentication:AllowedEmails must be configured with at least one entry.");

app.Run();

static string? GetConnectionString(WebApplicationBuilder builder, string connectionCfgName,
    string userCfgName, string passwordCfgName)
{
    var connectionString = builder.Configuration.GetConnectionString(connectionCfgName);
    if (builder.Environment.IsDevelopment())
    {
        var conStrBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            UserID = builder.Configuration[userCfgName],
            Password = builder.Configuration[passwordCfgName]
        };
        connectionString = conStrBuilder.ConnectionString;
    }
    return connectionString;
}
