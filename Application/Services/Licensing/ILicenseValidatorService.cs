using VisitorManagementSystem.Api.Application.Models.Licensing;

namespace VisitorManagementSystem.Api.Application.Services.Licensing;

public interface ILicenseValidatorService
{
    Task<LicenseValidationResult> ValidateCurrentLicenseAsync(CancellationToken ct = default);
    LicenseValidationResult GetCachedResult();
    void InvalidateCache();
    Task<LicenseValidationResult> ActivateLicenseAsync(byte[] licenseFileBytes, CancellationToken ct = default);
    Task DeactivateAsync(CancellationToken ct = default);
}
