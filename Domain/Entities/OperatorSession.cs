using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Tracks active operator/receptionist sessions for targeted notifications
/// </summary>
public class OperatorSession : AuditableEntity
{
    /// <summary>
    /// User ID of the operator
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// SignalR connection ID
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Location/group this operator is responsible for
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Operator's current status
    /// </summary>
    public OperatorStatus Status { get; set; } = OperatorStatus.Online;

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Session start time
    /// </summary>
    public DateTime SessionStart { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Session end time (null if active)
    /// </summary>
    public DateTime? SessionEnd { get; set; }

    /// <summary>
    /// Client IP address
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent information
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Additional session metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation properties
    /// </summary>
    public virtual User User { get; set; } = null!;
    public virtual Location? Location { get; set; }

    /// <summary>
    /// Update last activity timestamp
    /// </summary>
    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Set operator status
    /// </summary>
    public void SetStatus(OperatorStatus status)
    {
        Status = status;
        UpdateActivity();
    }

    /// <summary>
    /// End the session
    /// </summary>
    public void EndSession()
    {
        SessionEnd = DateTime.UtcNow;
        Status = OperatorStatus.Offline;
        UpdateModifiedOn();
    }

    /// <summary>
    /// Check if session is active
    /// </summary>
    public new bool IsActive => SessionEnd == null && Status != OperatorStatus.Offline;

    /// <summary>
    /// Calculate session duration
    /// </summary>
    public TimeSpan SessionDuration => 
        (SessionEnd ?? DateTime.UtcNow) - SessionStart;
}
