namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Invitation type enumeration
/// </summary>
public enum InvitationType
{
    /// <summary>
    /// Single visitor invitation
    /// </summary>
    Single = 0,

    /// <summary>
    /// Group invitation for multiple visitors
    /// </summary>
    Group = 1,

    /// <summary>
    /// Recurring invitation (daily, weekly, etc.)
    /// </summary>
    Recurring = 2,

    /// <summary>
    /// Walk-in visitor registration
    /// </summary>
    WalkIn = 3,

    /// <summary>
    /// Bulk imported invitation
    /// </summary>
    BulkImport = 4
}

/// <summary>
/// Approval decision enumeration
/// </summary>
public enum ApprovalDecision
{
    /// <summary>
    /// Pending approval
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Approved
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Rejected
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Escalated to higher authority
    /// </summary>
    Escalated = 3
}
