using System.Text.Json.Serialization;

namespace TimeTracker.Shared.Models.Identity;

public class AzureADClientSettings
{
    [JsonPropertyName("Authority")]
    public string? Authority { get; set; }

    [JsonPropertyName("ClientId")]
    public string? ClientId { get; set; }

    [JsonPropertyName("ValidateAuthority")]
    public bool ValidateAuthority { get; set; }

    [JsonPropertyName("Scope")]
    public string? Scope { get; set; }
}
