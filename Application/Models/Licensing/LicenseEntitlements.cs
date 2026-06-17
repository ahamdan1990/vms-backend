using System.Text.Json.Serialization;

namespace VisitorManagementSystem.Api.Application.Models.Licensing;

public class LicenseEntitlements
{
    [JsonPropertyName("maxUsers")]
    public int MaxUsers { get; set; } = 50;

    [JsonPropertyName("maxCameras")]
    public int MaxCameras { get; set; } = 10;

    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();
}
