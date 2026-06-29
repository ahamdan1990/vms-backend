// Application/DTOs/Users/ImportUserRowDto.cs
namespace VisitorManagementSystem.Api.Application.DTOs.Users;

/// <summary>
/// Represents a single row parsed from an import file (Excel or CSV).
/// All fields are nullable strings so the validator can produce precise per-cell errors.
/// </summary>
public class ImportUserRowDto
{
    /// <summary>1-based row number in the source file (excludes header row).</summary>
    public int RowNumber { get; set; }

    // ── Required fields ──────────────────────────────────────────────────────
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }

    // ── Phone ─────────────────────────────────────────────────────────────────
    public string? PhoneCountryCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PhoneType { get; set; }

    // ── Work info ─────────────────────────────────────────────────────────────
    public string? EmployeeId { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }

    // ── Preferences ───────────────────────────────────────────────────────────
    public string? TimeZone { get; set; }
    public string? Language { get; set; }

    // ── Address ───────────────────────────────────────────────────────────────
    public string? AddressType { get; set; }
    public string? Street1 { get; set; }
    public string? Street2 { get; set; }
    public string? City { get; set; }
    public string? Governorate { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // ── Photo ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Optional filename of a photo in the photos/ folder of the ZIP archive.
    /// e.g. "john_doe.jpg". Matched case-insensitively to extracted ZIP entries.
    /// </summary>
    public string? PhotoFile { get; set; }

    // ── Account settings ──────────────────────────────────────────────────────
    /// <summary>
    /// Raw cell value: "TRUE", "FALSE", or blank.
    /// Parsed during validation; stored as string to enable precise error messages.
    /// </summary>
    public string? MustChangePassword { get; set; }

    /// <summary>Raw cell value: "TRUE", "FALSE", or blank.</summary>
    public string? SendWelcomeEmail { get; set; }

    /// <summary>
    /// Raw cell value: "FollowGlobal", "AlwaysRequire", "AlwaysAutoApprove", or blank.
    /// Maps to bool? RequiresApprovalOverride on the User entity.
    /// </summary>
    public string? ApprovalOverride { get; set; }
}
