using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Configuration audit entity for tracking configuration changes
/// </summary>
public class ConfigurationAudit : AuditableEntity
{
    /// <summary>
    /// Reference to the system configuration
    /// </summary>
    public int SystemConfigurationId { get; set; }

    /// <summary>
    /// Configuration category at time of change
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Configuration key at time of change
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Previous value (null for new configurations)
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value
    /// </summary>
    [Required]
    public string NewValue { get; set; } = string.Empty;

    /// <summary>
    /// Type of change (Created, Updated, Deleted, Restored)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the change
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// IP address of the user making the change
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client making the change
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Session ID when the change was made
    /// </summary>
    [MaxLength(100)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Whether this change was made by an automated process
    /// </summary>
    public bool IsAutomated { get; set; } = false;

    /// <summary>
    /// Whether this change requires approval
    /// </summary>
    public bool RequiresApproval { get; set; } = false;

    /// <summary>
    /// Whether this change has been approved
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// User who approved the change
    /// </summary>
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// When the change was approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Additional metadata about the change (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation property to the system configuration
    /// </summary>
    public virtual SystemConfiguration SystemConfiguration { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user who approved the change
    /// </summary>
    public virtual User? ApprovedByUser { get; set; }

    /// <summary>
    /// Gets a description of the change
    /// </summary>
    public string ChangeDescription
    {
        get
        {
            return Action switch
            {
                "Created" => $"Created configuration {Category}.{Key} with value '{GetSafeValue(NewValue)}'",
                "Updated" => $"Updated configuration {Category}.{Key} from '{GetSafeValue(OldValue)}' to '{GetSafeValue(NewValue)}'",
                "Deleted" => $"Deleted configuration {Category}.{Key} (was '{GetSafeValue(OldValue)}')",
                "Restored" => $"Restored configuration {Category}.{Key} to '{GetSafeValue(NewValue)}'",
                _ => $"{Action} configuration {Category}.{Key}"
            };
        }
    }

    /// <summary>
    /// Gets a safe representation of a value (masks sensitive data)
    /// </summary>
    private string GetSafeValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "[empty]";

        // Check if this is likely sensitive data
        var sensitiveKeys = new[] { "password", "secret", "key", "token", "connectionstring" };
        var keyLower = Key.ToLower();

        if (sensitiveKeys.Any(sk => keyLower.Contains(sk)))
        {
            return value.Length > 4 ? $"{value[..2]}***{value[^2..]}" : "***";
        }

        // Truncate long values
        return value.Length > 50 ? $"{value[..47]}..." : value;
    }

    /// <summary>
    /// Approves the configuration change
    /// </summary>
    /// <param name="approvedBy">User ID who approved</param>
    public void Approve(int approvedBy)
    {
        IsApproved = true;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
        UpdateModifiedBy(approvedBy);
    }

    /// <summary>
    /// Gets the composite key for the configuration
    /// </summary>
    public string CompositeKey => $"{Category}.{Key}";
}
