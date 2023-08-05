using TimeTracker.Shared.Models.Identity;

namespace TimeTracker.API.Services;

public class ClientConfigurationManager : IClientConfigurationManager
{
    private readonly ClientConfiguration _clientConfig;
    public ClientConfigurationManager(IConfiguration configuration)
    {
        var aadServerSettings = configuration.GetSection("AzureAd").Get<AzureADServerSettings>();
        var clientConfig = configuration.GetSection("ClientConfiguration");

         _clientConfig = new ClientConfiguration()
         {
            AzureAD = new()
            {
                Authority = $"{aadServerSettings!.Instance}/{aadServerSettings.TenantId}",
                ClientId = clientConfig.GetValue<string>("ClientId"),
                ValidateAuthority = true,
                Scope = $"api://{aadServerSettings.ClientId}/API.Access"
            }
         };
    }
    public ClientConfiguration GetClientConfiguration()
    {
        return _clientConfig;
    }
}