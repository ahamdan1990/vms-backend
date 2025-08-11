using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents an event in the invitation timeline
/// </summary>
public class InvitationEvent : AuditableEntity
{
    /// <summary>
    /// Invitation this event belongs to
    /// </summary>
    [Required]
    public int InvitationId { get; set; }

    /// <summary>
    /// Event type
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event description
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User who triggered the event
    /// </summary>
    public int? TriggeredBy { get; set; }

    /// <summary>
    /// Additional event data (JSON)
    /// </summary>
    public string? EventData { get; set; }

    /// <summary>
    /// Event timestamp
    /// </summary>
    [Required]
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for the invitation
    /// </summary>
    public virtual Invitation Invitation { get; set; } = null!;

    /// <summary>
    /// Navigation property for the user who triggered the event
    /// </summary>
    public virtual User? TriggeredByUser { get; set; }

    /// <summary>
    /// Creates a new invitation event
    /// </summary>
    /// <param name="invitationId">Invitation ID</param>
    /// <param name="eventType">Event type</param>
    /// <param name="description">Event description</param>
    /// <param name="triggeredBy">User who triggered the event</param>
    /// <param name="eventData">Additional event data</param>
    /// <returns>New InvitationEvent</returns>
    public static InvitationEvent Create(int invitationId, string eventType, string description, int? triggeredBy = null, string? eventData = null)
    {
        return new InvitationEvent
        {
            InvitationId = invitationId,
            EventType = eventType,
            Description = description,
            TriggeredBy = triggeredBy,
            EventData = eventData,
            EventTimestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Common invitation event types
/// </summary>
public static class InvitationEventTypes
{
    public const string Created = "Created";
    public const string Submitted = "Submitted";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
    public const string CheckedIn = "CheckedIn";
    public const string CheckedOut = "CheckedOut";
    public const string Expired = "Expired";
    public const string Modified = "Modified";
    public const string Escalated = "Escalated";
    public const string ReminderSent = "ReminderSent";
    public const string QrCodeGenerated = "QrCodeGenerated";
}
