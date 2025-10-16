namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// Simplified visitor list DTO for display purposes
/// </summary>
public class VisitorListDto
{
    /// <summary>
    /// Visitor ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Visitor full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Visitor email address (masked if sensitive)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Company name
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// Phone number (masked)
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Phone country code
    /// </summary>
    public string? PhoneCountryCode { get; set; }

    /// <summary>
    /// Phone type (Mobile, Landline, etc.)
    /// </summary>
    public string? PhoneType { get; set; }

    /// <summary>
    /// Whether visitor is VIP
    /// </summary>
    public bool IsVip { get; set; }

    /// <summary>
    /// Whether visitor is blacklisted
    /// </summary>
    public bool IsBlacklisted { get; set; }

    /// <summary>
    /// Number of visits
    /// </summary>
    public int VisitCount { get; set; }

    /// <summary>
    /// Date of last visit
    /// </summary>
    public DateTime? LastVisitDate { get; set; }

    /// <summary>
    /// Profile photo URL
    /// </summary>
    public string? ProfilePhotoUrl { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Whether visitor is active
    /// </summary>
    public bool IsActive { get; set; }
}
