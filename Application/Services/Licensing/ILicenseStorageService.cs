using VisitorManagementSystem.Api.Application.Models.Licensing;

namespace VisitorManagementSystem.Api.Application.Services.Licensing;

public interface ILicenseStorageService
{
    Task<LicensePayload?> LoadLicenseAsync(CancellationToken ct = default);
    Task StoreLicenseAsync(LicensePayload payload, CancellationToken ct = default);
    Task ClearLicenseAsync(CancellationToken ct = default);
    bool LicenseFileExists();
    Task<bool> VerifyRegistryIntegrityAsync(LicensePayload payload);
    Task WriteRegistryChecksumAsync(LicensePayload payload);
    Task UpdateDatabaseRecordAsync(LicensePayload payload, bool[] componentScores, string? failureReason = null, CancellationToken ct = default);
    Task<bool> IsDatabaseRevokedAsync(string licenseId, CancellationToken ct = default);
}
