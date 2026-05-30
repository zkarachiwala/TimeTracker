using System.Reflection;
using TimeTracker.Web;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Scalar.AspNetCore;
using TimeTracker.Web.Data;
using TimeTracker.Web.Features.Auth;
using TimeTracker.Web.Features.Projects;
using TimeTracker.Web.Features.TimeEntries;
using TimeTracker.Web.Shared;
using TimeTracker.Shared.Entities;

var builder = WebApplication.CreateBuilder(args);

var timeTrackerConnection = GetConnectionString(builder, "TimeTrackerConnection", "DbUser", "DbPassword");
var identityConnection = GetConnectionString(builder, "IdentityConnection", "DbUser", "DbPassword");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddDbContext<TimeTrackerDataContext>(o => o.UseSqlServer(timeTrackerConnection));
builder.Services.AddDbContext<IdentityDataContext>(o => o.UseSqlServer(identityConnection));

builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<IdentityDataContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
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
