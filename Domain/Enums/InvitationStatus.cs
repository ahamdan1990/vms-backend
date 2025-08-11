namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Invitation status enumeration
/// </summary>
public enum InvitationStatus
{
    /// <summary>
    /// Draft - not yet submitted
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Submitted - awaiting approval
    /// </summary>
    Submitted = 1,

    /// <summary>
    /// Under review by admin
    /// </summary>
    UnderReview = 2,

    /// <summary>
    /// Approved - ready for visit
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Rejected by admin
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Cancelled by host or admin
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Expired - visit date passed
    /// </summary>
    Expired = 6,

    /// <summary>
    /// Active - visitor has checked in
    /// </summary>
    Active = 7,

    /// <summary>
    /// Completed - visit finished
    /// </summary>
    Completed = 8
}
