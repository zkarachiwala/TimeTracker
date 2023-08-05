using System.Text.Json.Serialization;

namespace TimeTracker.Shared.Models.Identity;

public class ClientConfiguration
{
    [JsonPropertyName("AzureAD")]
    public AzureADClientSettings? AzureAD { get; set; }
}