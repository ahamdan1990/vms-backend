using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a visit purpose/category in the system
/// </summary>
public class VisitPurpose : SoftDeleteEntity
{
    /// <summary>
    /// Purpose name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Purpose description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Purpose code for categorization
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Color code for UI display
    /// </summary>
    [MaxLength(7)]
    public string ColorCode { get; set; } = "#0078d4";

    /// <summary>
    /// Icon name for UI display
    /// </summary>
    [MaxLength(50)]
    public string? IconName { get; set; }    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; } = 1;

    /// <summary>
    /// Whether approval is required for this purpose
    /// </summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>
    /// Whether security clearance is required
    /// </summary>
    public bool RequiresSecurityClearance { get; set; } = false;

    /// <summary>
    /// Maximum visit duration in hours (0 = no limit)
    /// </summary>
    public int MaxDurationHours { get; set; } = 0;

    /// <summary>
    /// Whether background check is required
    /// </summary>
    public bool RequiresBackgroundCheck { get; set; } = false;

    /// <summary>
    /// Whether this is a default/system purpose
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Additional requirements/notes
    /// </summary>
    [MaxLength(1000)]
    public string? Requirements { get; set; }    /// <summary>
    /// Validates the visit purpose information
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidatePurpose()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Purpose name is required.");

        if (string.IsNullOrWhiteSpace(Code))
            errors.Add("Purpose code is required.");

        if (MaxDurationHours < 0)
            errors.Add("Maximum duration cannot be negative.");

        if (DisplayOrder < 1)
            errors.Add("Display order must be greater than zero.");

        // Validate color code format
        if (!string.IsNullOrEmpty(ColorCode) && !System.Text.RegularExpressions.Regex.IsMatch(ColorCode, @"^#[0-9A-Fa-f]{6}$"))
            errors.Add("Color code must be in valid hex format (#RRGGBB).");

        return errors;
    }

    /// <summary>
    /// Gets display text with additional info
    /// </summary>
    public string GetDisplayText()
    {
        var text = Name;
        if (RequiresApproval) text += " (Approval Required)";
        if (RequiresSecurityClearance) text += " (Security Clearance Required)";
        return text;
    }

    /// <summary>
    /// Navigation property for related invitations
    /// </summary>
    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
}
