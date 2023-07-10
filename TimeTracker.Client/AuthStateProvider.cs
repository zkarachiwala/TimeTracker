using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace TimeTracker.Client;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;

    public AuthStateProvider(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var authToken = await _localStorage.GetItemAsync<string>("authToken");
        AuthenticationState authState;
        if(string.IsNullOrWhiteSpace(authToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(authToken), "jwt")));
        }

        NotifyAuthenticationStateChanged(Task.FromResult(authState));

        return authState;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch(base64.Length % 4) {
            case 2 : base64 += "=="; break;
            case 3 : base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payLoad = jwt.Split(".")[1];
        var jsonBytes = ParseBase64WithoutPadding(payLoad);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        var claims = keyValuePairs!.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!));
        return claims;
    }
}
