using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

public class LicenseRecord : AuditableEntity
{
    public string LicenseId { get; set; } = string.Empty;
    public LicenseType LicenseType { get; set; }
    public LicenseStatus Status { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime LastValidatedAt { get; set; }
    public int ValidationCount { get; set; }
    public string ComponentScoresJson { get; set; } = "[]";
    public int ComponentMatchCount { get; set; }
    public string? FailureReason { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevocationReason { get; set; }
}
