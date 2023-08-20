using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TimeTracker.Client;

public class CustomAccountFactory : AccountClaimsPrincipalFactory<CustomUserAccount>
{
    private readonly HttpClient _httpClient;
    public CustomAccountFactory(IAccessTokenProviderAccessor accessor)//, HttpClient httpClient) 
        : base(accessor)
    {
        //_httpClient = httpClient;
    }

    public async override ValueTask<ClaimsPrincipal> CreateUserAsync(CustomUserAccount account, 
        RemoteAuthenticationUserOptions options)
    {
        var initialUser = await base.CreateUserAsync(account, options);
        if (initialUser.Identity.IsAuthenticated)
        {
            var userIdentity = (ClaimsIdentity)initialUser.Identity;

            //var result = await _httpClient.GetFromJsonAsync<List<string>>("api/account/role");

            foreach (var role in account.Roles)
            {
                userIdentity.AddClaim(new Claim("appRole", role));
            }
        }

        return initialUser;
    }
}