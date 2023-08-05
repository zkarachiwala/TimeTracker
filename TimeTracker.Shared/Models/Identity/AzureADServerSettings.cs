using System.Text.Json.Serialization;

namespace TimeTracker.Shared.Models.Identity;

public class AzureADServerSettings
{
    [JsonPropertyName("Instance")]
    public string? Instance { get; set; }

    [JsonPropertyName("Domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("TenantId")]
    public string? TenantId { get; set; }

    [JsonPropertyName("ClientId")]
    public string? ClientId { get; set; }
    
    [JsonPropertyName("CallbackPath")]
    public string? CallbackPath { get; set; }
}

