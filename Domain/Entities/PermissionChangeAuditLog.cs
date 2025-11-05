using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Audit log for tracking all permission changes (grants and revocations)
/// </summary>
public class PermissionChangeAuditLog
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Role ID if this change was for a role permission (nullable)
    /// </summary>
    public int? RoleId { get; set; }

    /// <summary>
    /// User ID if this change was for a user override (nullable - for future use)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Permission that was changed
    /// </summary>
    [Required]
    public int PermissionId { get; set; }

    /// <summary>
    /// Type of change: "Grant" or "Revoke"
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// User who made the change
    /// </summary>
    public int? ChangedBy { get; set; }

    /// <summary>
    /// When the change was made
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Reason for the change (optional)
    /// </summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>
    /// JSON snapshot of previous state
    /// </summary>
    public string? PreviousValue { get; set; }

    /// <summary>
    /// JSON snapshot of new state
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// IP address of the user who made the change
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    // Navigation properties

    /// <summary>
    /// Role that was modified (if applicable)
    /// </summary>
    public Role? Role { get; set; }

    /// <summary>
    /// User that was modified (if applicable - for future user overrides)
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Permission that was changed
    /// </summary>
    public Permission Permission { get; set; } = null!;

    /// <summary>
    /// User who made the change (nullable to avoid query filter issues)
    /// </summary>
    public User? ChangedByUser { get; set; }
}
