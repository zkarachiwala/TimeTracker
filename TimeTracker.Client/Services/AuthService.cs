using System.Net.Http.Json;
using Blazored.Toast;
using Blazored.Toast.Services;
using TimeTracker.Shared.Models.Account;

namespace TimeTracker.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IToastService _toastService;

    public AuthService(HttpClient httpClient, IToastService toastService)
    {
        _httpClient = httpClient;
        _toastService = toastService;
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