using System.Reflection;
using MudBlazor.Services;
using TimeTracker.Web;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;
using Serilog;
using TimeTracker.Web.Data;
using TimeTracker.Web.Features.Auth;
using TimeTracker.Web.Features.Clients;
using TimeTracker.Web.Features.Projects;
using TimeTracker.Web.Features.TimeEntries;
using TimeTracker.Web.Infrastructure;
using TimeTracker.Web.Shared;
using TimeTracker.Shared.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) =>
    config
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

var timeTrackerConnection = ConnectionStringBuilder.Build(builder, "TimeTrackerConnection", "DbUser", "DbPassword");
var identityConnection = ConnectionStringBuilder.Build(builder, "IdentityConnection", "DbUser", "DbPassword");

builder.Services.AddMudServices();
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddSingleton<UserSessionContextInterceptor>();
builder.Services.AddDbContextFactory<TimeTrackerDataContext>((sp, o) =>
{
    o.UseSqlServer(timeTrackerConnection);
    o.AddInterceptors(sp.GetRequiredService<UserSessionContextInterceptor>());
});
builder.Services.AddDbContext<IdentityDataContext>(o => o.UseSqlServer(identityConnection));

builder.Services.AddApplicationAuth(builder.Configuration);
builder.Services.AddApplicationRateLimiting(builder.Configuration);
builder.Services.AddApplicationHealthChecks();

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

// Satisfies DI for SSR prerender — WASM components guard actual calls with IsBrowser()
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
builder.Services.AddScoped<ITimeEntryQueryService, TimeEntryService>();
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
    app.MapDevEndpoints();
}

app.UseExceptionHandler();
app.UseHsts();
app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TimeTracker.Client.Features.Timer.Pages.TimerPage).Assembly);

app.MapApplicationHealthChecks();
app.MapControllers();
app.MapTimeEntryEndpoints();
app.MapProjectEndpoints();
app.MapClientEndpoints();
app.MapAuthEndpoints();

var allowedEmails = app.Configuration.GetSection("Authentication:AllowedEmails").Get<string[]>();
if (allowedEmails is null || allowedEmails.Length == 0)
    throw new InvalidOperationException("Authentication:AllowedEmails must be configured with at least one entry.");

app.Run();
