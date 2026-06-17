using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Services.Licensing;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// License management endpoints — intentionally exempt from LicenseEnforcementMiddleware
/// so that unlicensed systems can activate themselves.
/// </summary>
[ApiController]
[Route("api/license")]
public class LicenseController : ControllerBase
{
    private readonly ILicenseValidatorService _licenseValidator;
    private readonly IMachineFingerprintService _fingerprint;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(
        ILicenseValidatorService licenseValidator,
        IMachineFingerprintService fingerprint,
        ILogger<LicenseController> logger)
    {
        _licenseValidator = licenseValidator;
        _fingerprint = fingerprint;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/license/status
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Returns current license status. No authentication required.</summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        var result = _licenseValidator.GetCachedResult();

        return Ok(new
        {
            isValid = result.IsValid,
            status = result.Status.ToString(),
            licenseType = result.LicenseType?.ToString(),
            customerEmail = result.CustomerEmail,
            issuedAt = result.IssuedAt,
            expiresAt = result.ExpiresAt,
            daysRemaining = result.ExpiresAt.HasValue
                ? Math.Max(0, (int)(result.ExpiresAt.Value - DateTime.UtcNow).TotalDays)
                : (int?)null,
            reason = result.FailureReason,
            entitlements = result.Entitlements == null ? null : new
            {
                maxUsers = result.Entitlements.MaxUsers,
                maxCameras = result.Entitlements.MaxCameras,
                features = result.Entitlements.Features
            }
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/license/fingerprint
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the machine fingerprint hash for this server.
    /// The admin sends this to the vendor to obtain a license file.
    /// No authentication required.
    /// </summary>
    [HttpGet("fingerprint")]
    [AllowAnonymous]
    public IActionResult GetFingerprint()
    {
        try
        {
            var components = _fingerprint.CollectComponents();
            var fp = _fingerprint.ComputeFingerprint();

            return Ok(new
            {
                fingerprint = fp.FullHash,
                components = new
                {
                    biosUuid = MaskValue(components.BiosUuid),
                    motherboardSerial = MaskValue(components.MotherboardSerial),
                    cpuProcessorId = MaskValue(components.CpuProcessorId),
                    windowsProductId = MaskValue(components.WindowsProductId),
                    primaryMacAddress = MaskValue(components.PrimaryMacAddress)
                },
                instructions = "Send the 'fingerprint' value to your VMS vendor along with your email address to obtain a license file."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect machine fingerprint");
            return StatusCode(500, new { error = "Failed to collect machine fingerprint." });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // POST /api/license/activate
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Activates the system with a .vmslic license file.
    /// Validates signature, hardware binding, and stores in triple-redundant storage.
    /// No authentication required (system may not be usable yet without a license).
    /// </summary>
    [HttpPost("activate")]
    [AllowAnonymous]
    [RequestSizeLimit(1_048_576)] // 1 MB max
    public async Task<IActionResult> Activate(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No license file provided." });

        if (!file.FileName.EndsWith(".vmslic", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "File must be a .vmslic license file." });

        if (file.Length > 65536)
            return BadRequest(new { error = "License file is too large." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var result = await _licenseValidator.ActivateLicenseAsync(bytes, ct);

        if (!result.IsValid)
        {
            _logger.LogWarning("License activation failed: {Status} — {Reason}", result.Status, result.FailureReason);
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new
            {
                error = "License activation failed.",
                status = result.Status.ToString(),
                reason = result.FailureReason
            });
        }

        _logger.LogInformation("License activated by request from {IP}", HttpContext.Connection.RemoteIpAddress);
        return Ok(new
        {
            message = "License activated successfully.",
            licenseType = result.LicenseType?.ToString(),
            expiresAt = result.ExpiresAt,
            customerEmail = result.CustomerEmail
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    // POST /api/license/deactivate
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Deactivates (removes) the current license. Requires admin role.</summary>
    [HttpPost("deactivate")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Deactivate(CancellationToken ct)
    {
        await _licenseValidator.DeactivateAsync(ct);
        _logger.LogWarning("License deactivated by {User}", User.Identity?.Name);
        return Ok(new { message = "License deactivated. System is now in restricted mode." });
    }

    // ─────────────────────────────────────────────────────────────────────
    // POST /api/license/refresh
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Forces an immediate re-validation of the license from storage.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        _licenseValidator.InvalidateCache();
        var result = await _licenseValidator.ValidateCurrentLicenseAsync(ct);
        return Ok(new
        {
            isValid = result.IsValid,
            status = result.Status.ToString(),
            reason = result.FailureReason
        });
    }

    // ─────────────────────────────────────────────────────────────────────

    private static string MaskValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return "(not available)";
        if (value.Length <= 4) return "****";
        return value[..4] + new string('*', Math.Min(value.Length - 4, 8));
    }
}
