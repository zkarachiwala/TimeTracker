using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using TimeTracker.Wasm.Features.Clients;
using TimeTracker.Wasm.Features.Projects;
using TimeTracker.Wasm.Features.TimeEntries;
using TimeTracker.Contracts.Features.Clients;
using TimeTracker.Contracts.Features.Projects;
using TimeTracker.Contracts.Features.TimeEntries;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices(config =>
    config.PopoverOptions.CheckForPopoverProvider = false);

builder.Services.AddScoped(sp =>
    new HttpClient(new TimeTracker.Wasm.CookieCredentialHandler())
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

builder.Services.AddScoped<ITimeEntryService, HttpTimeEntryService>();
builder.Services.AddScoped<IProjectService, HttpProjectService>();
builder.Services.AddScoped<IClientService, HttpClientService>();

await builder.Build().RunAsync();
