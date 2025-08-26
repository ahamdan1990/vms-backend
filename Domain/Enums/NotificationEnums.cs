namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Types of notification alerts in the system
/// </summary>
public enum NotificationAlertType
{
    /// <summary>
    /// Visitor arrival detected by FR system
    /// </summary>
    VisitorArrival = 1,

    /// <summary>
    /// VIP visitor detected
    /// </summary>
    VipArrival = 2,

    /// <summary>
    /// Unknown face detected at entrance
    /// </summary>
    UnknownFace = 3,

    /// <summary>
    /// Blacklisted person detected
    /// </summary>
    BlacklistAlert = 4,

    /// <summary>
    /// Visitor check-in completed
    /// </summary>
    VisitorCheckedIn = 5,

    /// <summary>
    /// Visitor check-out completed
    /// </summary>
    VisitorCheckedOut = 6,

    /// <summary>
    /// New invitation requires approval
    /// </summary>
    InvitationPendingApproval = 7,

    /// <summary>
    /// Invitation was approved
    /// </summary>
    InvitationApproved = 8,

    /// <summary>
    /// Invitation was rejected
    /// </summary>
    InvitationRejected = 9,

    /// <summary>
    /// System error or warning
    /// </summary>
    SystemAlert = 10,

    /// <summary>
    /// FR system connectivity issue
    /// </summary>
    FRSystemOffline = 11,

    /// <summary>
    /// Capacity limit reached or exceeded
    /// </summary>
    CapacityAlert = 12,

    /// <summary>
    /// Emergency evacuation or lockdown
    /// </summary>
    EmergencyAlert = 13,

    /// <summary>
    /// Manual override performed
    /// </summary>
    ManualOverride = 14,

    /// <summary>
    /// Visitor overstay alert
    /// </summary>
    VisitorOverstay = 15,

    /// <summary>
    /// Badge printing issue
    /// </summary>
    BadgePrintingError = 16,

    /// <summary>
    /// Custom user-defined alert
    /// </summary>
    Custom = 99
}

/// <summary>
/// Alert priority levels
/// </summary>
public enum AlertPriority
{
    /// <summary>
    /// Low priority - informational
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium priority - standard alerts
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High priority - requires attention
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical priority - immediate action required
    /// </summary>
    Critical = 4,

    /// <summary>
    /// Emergency priority - security/safety issue
    /// </summary>
    Emergency = 5
}

/// <summary>
/// Operator session status
/// </summary>
public enum OperatorStatus
{
    /// <summary>
    /// Operator is offline/disconnected
    /// </summary>
    Offline = 0,

    /// <summary>
    /// Operator is online and available
    /// </summary>
    Online = 1,

    /// <summary>
    /// Operator is busy processing visitors
    /// </summary>
    Busy = 2,

    /// <summary>
    /// Operator is on break/away
    /// </summary>
    Away = 3,

    /// <summary>
    /// Operator requires assistance
    /// </summary>
    NeedAssistance = 4
}

/// <summary>
/// Actions to take when escalating alerts
/// </summary>
public enum EscalationAction
{
    /// <summary>
    /// Escalate to a specific role/group
    /// </summary>
    EscalateToRole = 1,

    /// <summary>
    /// Escalate to a specific user
    /// </summary>
    EscalateToUser = 2,

    /// <summary>
    /// Send email notification
    /// </summary>
    SendEmail = 3,

    /// <summary>
    /// Send SMS notification
    /// </summary>
    SendSMS = 4,

    /// <summary>
    /// Create a high priority alert
    /// </summary>
    CreateHighPriorityAlert = 5,

    /// <summary>
    /// Log as critical event
    /// </summary>
    LogCriticalEvent = 6
}
