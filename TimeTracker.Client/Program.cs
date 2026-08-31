using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TimeTracker.Client.Features.Admin;
using TimeTracker.Client.Features.Clients;
using TimeTracker.Client.Features.Projects;
using TimeTracker.Client.Features.TimeEntries;
using TimeTracker.Client.Features.Timer;
using TimeTracker.Client.Shared;
using TimeTracker.Contracts.Features.Admin;
using TimeTracker.Contracts.Features.Clients;
using TimeTracker.Contracts.Features.Projects;
using TimeTracker.Contracts.Features.TimeEntries;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddMudServices();
builder.Services.AddScoped<ILocalStore, LocalStore>();
builder.Services.AddScoped<PendingStopSync>();

#if !SHOWCASE
// Reads the AuthenticationState the server already computed (via AddAuthenticationStateSerialization
// in TimeTracker.Web/Program.cs) out of PersistentComponentState instead of re-checking over the
// network. See ADR-037.
builder.Services.AddAuthenticationStateDeserialization();
#endif

#if SHOWCASE
builder.RootComponents.Add<TimeTracker.Client.Routes>("#app");
builder.RootComponents.Add<Microsoft.AspNetCore.Components.Web.HeadOutlet>("head::after");
builder.Services.AddScoped<AuthenticationStateProvider, TimeTracker.Client.Mock.MockAuthenticationStateProvider>();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<TimeTracker.Client.Mock.MockDataStore>();
builder.Services.AddScoped<ITimeEntryService, TimeTracker.Client.Mock.MockTimeEntryService>();
builder.Services.AddScoped<IProjectService, TimeTracker.Client.Mock.MockProjectService>();
builder.Services.AddScoped<IClientService, TimeTracker.Client.Mock.MockClientService>();
builder.Services.AddScoped<IUserManagementService, TimeTracker.Client.Mock.MockUserManagementService>();
#else
builder.Services.AddScoped(sp =>
    new HttpClient(new TimeTracker.Client.CookieCredentialHandler())
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });
builder.Services.AddScoped<ITimeEntryService, HttpTimeEntryService>();
builder.Services.AddScoped<IProjectService, HttpProjectService>();
builder.Services.AddScoped<IClientService, HttpClientService>();
builder.Services.AddScoped<TimeTracker.Contracts.Features.Admin.IUserManagementService, HttpUserManagementService>();
#endif

await builder.Build().RunAsync();
