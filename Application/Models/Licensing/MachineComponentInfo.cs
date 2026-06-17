namespace VisitorManagementSystem.Api.Application.Models.Licensing;

public class MachineComponentInfo
{
    public string BiosUuid { get; set; } = string.Empty;
    public string MotherboardSerial { get; set; } = string.Empty;
    public string CpuProcessorId { get; set; } = string.Empty;
    public string WindowsProductId { get; set; } = string.Empty;
    public string PrimaryMacAddress { get; set; } = string.Empty;

    public MachineFingerprintData ToFingerprintData(int requiredMatches = 3)
    {
        var components = new[] { BiosUuid, MotherboardSerial, CpuProcessorId, WindowsProductId, PrimaryMacAddress };
        var componentHashes = components
            .Select(c => ComputeComponentHash(c))
            .ToArray();

        var fullHash = ComputeFullHash(components);

        return new MachineFingerprintData
        {
            FullHash = fullHash,
            ComponentHashes = componentHashes,
            RequiredMatches = requiredMatches
        };
    }

    private static string ComputeComponentHash(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(normalized);
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }

    private static string ComputeFullHash(string[] components)
    {
        var joined = string.Join("|", components.Select(c => (c ?? string.Empty).Trim().ToUpperInvariant()));
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(joined);
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }
}
