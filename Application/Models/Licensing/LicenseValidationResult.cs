using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Models.Licensing;

public class LicenseValidationResult
{
    public bool IsValid { get; init; }
    public LicenseStatus Status { get; init; }
    public string? FailureReason { get; init; }
    public LicenseType? LicenseType { get; init; }
    public DateTime? IssuedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? CustomerEmail { get; init; }
    public LicenseEntitlements? Entitlements { get; init; }
    public int ComponentMatchCount { get; init; }
    public bool[] ComponentScores { get; init; } = Array.Empty<bool>();

    public static LicenseValidationResult Success(LicensePayload payload, bool[] componentScores) => new()
    {
        IsValid = true,
        Status = LicenseStatus.Valid,
        LicenseType = Enum.TryParse<LicenseType>(payload.LicenseType, true, out var lt) ? lt : Domain.Enums.LicenseType.Full,
        IssuedAt = payload.IssuedAt,
        ExpiresAt = payload.ExpiresAt,
        CustomerEmail = payload.CustomerEmail,
        Entitlements = payload.Entitlements,
        ComponentMatchCount = componentScores.Count(s => s),
        ComponentScores = componentScores
    };

    public static LicenseValidationResult Failure(LicenseStatus status, string reason) => new()
    {
        IsValid = false,
        Status = status,
        FailureReason = reason
    };

    public static LicenseValidationResult NotActivated() => new()
    {
        IsValid = false,
        Status = LicenseStatus.NotActivated,
        FailureReason = "No license file found. Please activate the system."
    };

    public static LicenseValidationResult DevMode() => new()
    {
        IsValid = true,
        Status = LicenseStatus.Valid,
        LicenseType = Domain.Enums.LicenseType.Full,
        FailureReason = null,
        Entitlements = new LicenseEntitlements
        {
            MaxUsers = int.MaxValue,
            MaxCameras = int.MaxValue,
            Features = ["cameras", "faceRecognition", "civilDefense"]
        }
    };
}
