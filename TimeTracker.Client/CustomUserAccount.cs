using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Text.Json.Serialization;

namespace TimeTracker.Client;

public class CustomUserAccount : RemoteUserAccount
{
    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = Array.Empty<string>();
}