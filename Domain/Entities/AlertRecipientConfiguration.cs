using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Configures which users or roles receive specific alert types.
/// Supports both role-based and user-specific targeting.
/// </summary>
public class AlertRecipientConfiguration : AuditableEntity
{
    /// <summary>The alert type this rule applies to</summary>
    public NotificationAlertType AlertType { get; set; }

    /// <summary>"Role" or "User"</summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>Role name (e.g. "Administrator") — populated when TargetType == "Role"</summary>
    public string? TargetRole { get; set; }

    /// <summary>User ID — populated when TargetType == "User"</summary>
    public int? TargetUserId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Description { get; set; }

    // Navigation
    public virtual User? TargetUser { get; set; }

    // ──────────────────────────────────────────
    // Factory helpers

    public static AlertRecipientConfiguration ForRole(
        NotificationAlertType alertType, string role, int createdBy, string? description = null)
        => new()
        {
            AlertType = alertType,
            TargetType = "Role",
            TargetRole = role,
            IsEnabled = true,
            Description = description,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };

    public static AlertRecipientConfiguration ForUser(
        NotificationAlertType alertType, int userId, int createdBy, string? description = null)
        => new()
        {
            AlertType = alertType,
            TargetType = "User",
            TargetUserId = userId,
            IsEnabled = true,
            Description = description,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow
        };
}
