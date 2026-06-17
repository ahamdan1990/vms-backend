using System.Text.Json;
using System.Text.Json.Serialization;

namespace VisitorManagementSystem.Api.Application.Models.Licensing;

public class LicensePayload
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("licenseType")]
    public string LicenseType { get; set; } = string.Empty;

    [JsonPropertyName("issuedAt")]
    public DateTime IssuedAt { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("notBefore")]
    public DateTime NotBefore { get; set; }

    [JsonPropertyName("machineFingerprint")]
    public MachineFingerprintData MachineFingerprint { get; set; } = new();

    [JsonPropertyName("entitlements")]
    public LicenseEntitlements Entitlements { get; set; } = new();

    [JsonPropertyName("issuedBy")]
    public string IssuedBy { get; set; } = "VMS-Vendor-v1";

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    public string ToCanonicalJson()
    {
        // Serialize everything except signature, with sorted keys, compact
        var doc = new Dictionary<string, object?>
        {
            ["version"] = Version,
            ["licenseId"] = LicenseId,
            ["customerEmail"] = CustomerEmail,
            ["licenseType"] = LicenseType,
            ["issuedAt"] = IssuedAt.ToString("O"),
            ["expiresAt"] = ExpiresAt.ToString("O"),
            ["notBefore"] = NotBefore.ToString("O"),
            ["machineFingerprint"] = new Dictionary<string, object?>
            {
                ["fullHash"] = MachineFingerprint.FullHash,
                ["componentHashes"] = MachineFingerprint.ComponentHashes,
                ["requiredMatches"] = MachineFingerprint.RequiredMatches
            },
            ["entitlements"] = new Dictionary<string, object?>
            {
                ["maxUsers"] = Entitlements.MaxUsers,
                ["maxCameras"] = Entitlements.MaxCameras,
                ["features"] = Entitlements.Features
            },
            ["issuedBy"] = IssuedBy
        };

        return JsonSerializer.Serialize(doc, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = null
        });
    }
}
