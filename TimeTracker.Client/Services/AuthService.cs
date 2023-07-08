using System.Net.Http.Json;
using TimeTracker.Shared.Models.Account;

namespace TimeTracker.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task Register(AccountRegistrationRequest request)
    {
        await _httpClient.PostAsJsonAsync("api/account", request);
    }
}