using System.Text.Json.Serialization;

namespace VisitorManagementSystem.Api.Application.Models.Licensing;

public class MachineFingerprintData
{
    [JsonPropertyName("fullHash")]
    public string FullHash { get; set; } = string.Empty;

    [JsonPropertyName("componentHashes")]
    public string[] ComponentHashes { get; set; } = Array.Empty<string>();

    [JsonPropertyName("requiredMatches")]
    public int RequiredMatches { get; set; } = 3;
}
