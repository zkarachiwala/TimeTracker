using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TimeTracker.Client.Features.Auth;
using TimeTracker.Client.Features.Clients;
using TimeTracker.Client.Features.Projects;
using TimeTracker.Client.Features.TimeEntries;
using TimeTracker.Contracts.Features.Clients;
using TimeTracker.Contracts.Features.Projects;
using TimeTracker.Contracts.Features.TimeEntries;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddMudServices();

#if SHOWCASE
builder.Services.AddScoped<AuthenticationStateProvider, TimeTracker.Client.Mock.MockAuthenticationStateProvider>();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<TimeTracker.Client.Mock.MockDataStore>();
builder.Services.AddScoped<ITimeEntryService, TimeTracker.Client.Mock.MockTimeEntryService>();
builder.Services.AddScoped<IProjectService, TimeTracker.Client.Mock.MockProjectService>();
builder.Services.AddScoped<IClientService, TimeTracker.Client.Mock.MockClientService>();
#else
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
builder.Services.AddScoped(sp =>
    new HttpClient(new TimeTracker.Client.CookieCredentialHandler())
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });
builder.Services.AddScoped<ITimeEntryService, HttpTimeEntryService>();
builder.Services.AddScoped<IProjectService, HttpProjectService>();
builder.Services.AddScoped<IClientService, HttpClientService>();
#endif

await builder.Build().RunAsync();
