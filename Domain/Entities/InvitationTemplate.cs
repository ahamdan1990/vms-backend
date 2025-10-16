using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a template for creating invitations
/// </summary>
public class InvitationTemplate : SoftDeleteEntity
{
    /// <summary>
    /// Template name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Template category
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Default subject template
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Default message template
    /// </summary>
    [MaxLength(1000)]
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// Default visit purpose ID
    /// </summary>
    public int? DefaultVisitPurposeId { get; set; }

    /// <summary>
    /// Default location ID
    /// </summary>
    public int? DefaultLocationId { get; set; }

    /// <summary>
    /// Default visit duration in hours
    /// </summary>
    [Range(0.25, 24)]
    public double DefaultDurationHours { get; set; } = 2.0;

    /// <summary>
    /// Whether approval is required by default
    /// </summary>
    public bool DefaultRequiresApproval { get; set; } = true;

    /// <summary>
    /// Whether escort is required by default
    /// </summary>
    public bool DefaultRequiresEscort { get; set; } = false;

    /// <summary>
    /// Whether badge is required by default
    /// </summary>
    public bool DefaultRequiresBadge { get; set; } = true;

    /// <summary>
    /// Default special instructions
    /// </summary>
    [MaxLength(500)]
    public string? DefaultSpecialInstructions { get; set; }

    /// <summary>
    /// Whether template is shared with all users
    /// </summary>
    public bool IsShared { get; set; } = false;

    /// <summary>
    /// Whether template is system-defined
    /// </summary>
    public bool IsSystemTemplate { get; set; } = false;

    /// <summary>
    /// Usage count (how many times this template has been used)
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Last used date
    /// </summary>
    public DateTime? LastUsedOn { get; set; }

    /// <summary>
    /// Navigation property for default visit purpose
    /// </summary>
    public virtual VisitPurpose? DefaultVisitPurpose { get; set; }

    /// <summary>
    /// Navigation property for default location
    /// </summary>
    public virtual Location? DefaultLocation { get; set; }

    /// <summary>
    /// Increments the usage count
    /// </summary>
    public void IncrementUsage()
    {
        UsageCount++;
        LastUsedOn = DateTime.UtcNow;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Gets rendered subject with placeholders replaced
    /// </summary>
    /// <param name="placeholders">Dictionary of placeholder values</param>
    /// <returns>Rendered subject</returns>
    public string RenderSubject(Dictionary<string, string> placeholders)
    {
        var rendered = SubjectTemplate;
        foreach (var placeholder in placeholders)
        {
            rendered = rendered.Replace($"{{{placeholder.Key}}}", placeholder.Value);
        }
        return rendered;
    }

    /// <summary>
    /// Gets rendered message with placeholders replaced
    /// </summary>
    /// <param name="placeholders">Dictionary of placeholder values</param>
    /// <returns>Rendered message</returns>
    public string? RenderMessage(Dictionary<string, string> placeholders)
    {
        if (string.IsNullOrEmpty(MessageTemplate))
            return null;

        var rendered = MessageTemplate;
        foreach (var placeholder in placeholders)
        {
            rendered = rendered.Replace($"{{{placeholder.Key}}}", placeholder.Value);
        }
        return rendered;
    }
}
