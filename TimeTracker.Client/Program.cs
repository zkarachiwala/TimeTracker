using Blazored.LocalStorage;
using Blazored.Toast;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TimeTracker.Client;
using TimeTracker.Client.Services;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddBlazoredToast();
builder.Services.AddBlazoredLocalStorage();
//builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
//builder.Services.AddAuthorizationCore();

/*
    Load clientconfiguration.json
*/
using var http = new HttpClient() 
{ 
  BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
};
using var response = await http.GetAsync("clientconfiguration.json");
using var stream = await response.Content.ReadAsStreamAsync();
builder.Configuration.AddJsonStream(stream);
/*
** Add a HttpClient for REST APIs to the app.
*/
var name = "TimeTracker.ServerAPI";
var aadScope = builder.Configuration["AzureAd:Scope"];
builder.Services
    .AddHttpClient(name, client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
builder.Services
    .AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(name));

/*
** Add support for Azure Active Directory
*/
builder.Services
    .AddMsalAuthentication<RemoteAuthenticationState, CustomUserAccount>(options =>
    {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        options.ProviderOptions.DefaultAccessTokenScopes.Add(aadScope);
        options.UserOptions.RoleClaim = "appRole";
        options.ProviderOptions.LoginMode = "redirect";
    }).AddAccountClaimsPrincipalFactory<RemoteAuthenticationState, CustomUserAccount,
    CustomAccountFactory>();

await builder.Build().RunAsync();
