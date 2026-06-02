using System.Reflection;
using MudBlazor.Services;
using TimeTracker.Web;
using Mapster;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Scalar.AspNetCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Dev;
using TimeTracker.Web.Features.Auth;
using TimeTracker.Web.Features.Clients;
using TimeTracker.Web.Features.Projects;
using TimeTracker.Web.Features.TimeEntries;
using TimeTracker.Web.Shared;
using TimeTracker.Shared.Entities;

var builder = WebApplication.CreateBuilder(args);

var timeTrackerConnection = GetConnectionString(builder, "TimeTrackerConnection", "DbUser", "DbPassword");
var identityConnection = GetConnectionString(builder, "IdentityConnection", "DbUser", "DbPassword");

builder.Services.AddMudServices(config =>
{
    // MudPopoverProvider IS in MainLayout. The default check fires too early
    // in Blazor 9 per-page InteractiveServer mode (race between provider
    // registration and component initialization). Disable the eager check.
    config.PopoverOptions.CheckForPopoverProvider = false;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/login";
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
});

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IExternalLoginService, ExternalLoginService>();

var app = builder.Build();

TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.MapPost("/api/dev/seed", async (
        IDbContextFactory<TimeTrackerDataContext> ctxFactory,
        UserManager<User> userManager) =>
    {
        var result = await DevDataSeeder.SeedAsync(ctxFactory, userManager);
        return Results.Ok(result);
    }).AllowAnonymous();

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
    }).AllowAnonymous();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();
app.MapTimeEntryEndpoints();
app.MapProjectEndpoints();
app.MapClientEndpoints();
app.MapAuthEndpoints();

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
