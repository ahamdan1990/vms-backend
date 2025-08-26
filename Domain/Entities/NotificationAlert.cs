using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a system-generated notification alert
/// </summary>
public class NotificationAlert : AuditableEntity
{
    /// <summary>
    /// Alert title/subject
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Alert message content
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Alert type (determines routing and priority)
    /// </summary>
    public NotificationAlertType Type { get; set; }

    /// <summary>
    /// Alert priority level
    /// </summary>
    public AlertPriority Priority { get; set; } = AlertPriority.Medium;

    /// <summary>
    /// Target user role for this alert (null = all roles)
    /// </summary>
    [MaxLength(50)]
    public string? TargetRole { get; set; }

    /// <summary>
    /// Specific target user ID (null = role-based routing)
    /// </summary>
    public int? TargetUserId { get; set; }

    /// <summary>
    /// Target location/group for operators
    /// </summary>
    public int? TargetLocationId { get; set; }

    /// <summary>
    /// Related entity type (Visitor, Invitation, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Related entity ID
    /// </summary>
    public int? RelatedEntityId { get; set; }

    /// <summary>
    /// Alert payload data (JSON)
    /// </summary>
    public string? PayloadData { get; set; }

    /// <summary>
    /// Whether alert has been read/acknowledged
    /// </summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>
    /// Who acknowledged the alert
    /// </summary>
    public int? AcknowledgedBy { get; set; }

    /// <summary>
    /// When alert was acknowledged
    /// </summary>
    public DateTime? AcknowledgedOn { get; set; }

    /// <summary>
    /// Alert expiry time (auto-dismiss)
    /// </summary>
    public DateTime? ExpiresOn { get; set; }

    /// <summary>
    /// Whether alert has been sent via external channels (email/SMS)
    /// </summary>
    public bool SentExternally { get; set; }

    /// <summary>
    /// External send timestamp
    /// </summary>
    public DateTime? SentExternallyOn { get; set; }

    /// <summary>
    /// Navigation properties
    /// </summary>
    public virtual User? TargetUser { get; set; }
    public virtual User? AcknowledgedByUser { get; set; }
    public virtual Location? TargetLocation { get; set; }

    /// <summary>
    /// Factory method to create FR-related alerts
    /// </summary>
    public static NotificationAlert CreateFRAlert(string title, string message, NotificationAlertType type, 
        AlertPriority priority, string? targetRole = null, int? targetUserId = null, int? targetLocationId = null, 
        string? relatedEntityType = null, int? relatedEntityId = null, string? payloadData = null, int? createdBy = null)
    {
        var alert = new NotificationAlert
        {
            Title = title,
            Message = message,
            Type = type,
            Priority = priority,
            TargetRole = targetRole,
            TargetUserId = targetUserId,
            TargetLocationId = targetLocationId,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            PayloadData = payloadData,
            ExpiresOn = DateTime.UtcNow.AddHours(24) // Auto-expire after 24 hours
        };

        if (createdBy.HasValue)
            alert.SetCreatedBy(createdBy.Value);

        return alert;
    }

    /// <summary>
    /// Acknowledge the alert
    /// </summary>
    public void Acknowledge(int acknowledgedBy)
    {
        IsAcknowledged = true;
        AcknowledgedBy = acknowledgedBy;
        AcknowledgedOn = DateTime.UtcNow;
        UpdateModifiedBy(acknowledgedBy);
    }

    /// <summary>
    /// Mark as sent externally
    /// </summary>
    public void MarkSentExternally()
    {
        SentExternally = true;
        SentExternallyOn = DateTime.UtcNow;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Check if alert is expired
    /// </summary>
    public bool IsExpired => ExpiresOn.HasValue && DateTime.UtcNow > ExpiresOn.Value;
}
