using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Notifications;

/// <summary>
/// Data transfer object for notification alerts
/// </summary>
public class NotificationAlertDto
{
    /// <summary>
    /// Alert ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Alert title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Alert message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Alert type
    /// </summary>
    public NotificationAlertType Type { get; set; }

    /// <summary>
    /// Alert priority
    /// </summary>
    public AlertPriority Priority { get; set; }

    /// <summary>
    /// Target user role
    /// </summary>
    public string? TargetRole { get; set; }

    /// <summary>
    /// Target user ID
    /// </summary>
    public int? TargetUserId { get; set; }

    /// <summary>
    /// Target location ID
    /// </summary>
    public int? TargetLocationId { get; set; }

    /// <summary>
    /// Location name
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// Is acknowledged
    /// </summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>
    /// Acknowledged by user ID
    /// </summary>
    public int? AcknowledgedBy { get; set; }

    /// <summary>
    /// Acknowledged by user name
    /// </summary>
    public string? AcknowledgedByName { get; set; }

    /// <summary>
    /// Acknowledged on date
    /// </summary>
    public DateTime? AcknowledgedOn { get; set; }

    /// <summary>
    /// Sent externally
    /// </summary>
    public bool SentExternally { get; set; }

    /// <summary>
    /// Sent externally on date
    /// </summary>
    public DateTime? SentExternallyOn { get; set; }

    /// <summary>
    /// Related entity type
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Related entity ID
    /// </summary>
    public int? RelatedEntityId { get; set; }

    /// <summary>
    /// Payload data
    /// </summary>
    public string? PayloadData { get; set; }

    /// <summary>
    /// Expiration date
    /// </summary>
    public DateTime? ExpiresOn { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Modified date
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Time since created (for display purposes)
    /// </summary>
    public TimeSpan TimeSinceCreated => DateTime.UtcNow - CreatedOn;

    /// <summary>
    /// Is expired
    /// </summary>
    public bool IsExpired => ExpiresOn.HasValue && ExpiresOn.Value <= DateTime.UtcNow;
}
