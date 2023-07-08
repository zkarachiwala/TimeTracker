using System.Net.Http.Json;
using Blazored.Toast;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.Components;
using TimeTracker.Shared.Models.Account;
using TimeTracker.Shared.Models.Login;

namespace TimeTracker.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IToastService _toastService;
    private readonly NavigationManager _navigationManager;

    public AuthService(HttpClient httpClient, IToastService toastService, NavigationManager navigationManager)
    {
        _httpClient = httpClient;
        _toastService = toastService;
        _navigationManager = navigationManager;
    }

    public async Task Login(LoginRequest request)
    {
        var result = await _httpClient.PostAsJsonAsync("api/login", request);
        if (result is not null)
        {
            var response = await result.Content.ReadFromJsonAsync<LoginResponse>();
            if(!response.IsSuccessful && response.Error is not null)
            {
                _toastService.ShowError(response.Error);
            }
            else if(!response.IsSuccessful)
            {
                _toastService.ShowError("An unexpected error has occured");
            }
            else
            {
                _toastService.ShowSuccess("Login successful! :)");
                _navigationManager.NavigateTo("timeentries");
            }
        }        
    }

    public async Task Register(AccountRegistrationRequest request)
    {
        var result =  await _httpClient.PostAsJsonAsync("api/account", request);
        if (result is not null)
        {
            var response = await result.Content.ReadFromJsonAsync<AccountRegistrationResponse>();
            if(!response.IsSuccessful && response.Errors is not null)
            {
                foreach(var error in response.Errors)
                {
                    _toastService.ShowError(error);
                }
            }
            else if(!response.IsSuccessful)
            {
                _toastService.ShowError("An unexpected error has occured");
            }
            else
            {
                _toastService.ShowSuccess("Registration successful!  You may login now. :)");
            }
        }
    }
}