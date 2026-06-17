namespace VisitorManagementSystem.Api.Domain.Enums;

public enum LicenseStatus
{
    NotActivated = 0,
    Valid = 1,
    Expired = 2,
    Tampered = 3,
    HardwareMismatch = 4,
    Revoked = 5,
    Invalid = 6
}
