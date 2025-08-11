using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for invitation operations
/// </summary>
public interface IInvitationRepository : IGenericRepository<Invitation>
{
    /// <summary>
    /// Gets invitations by visitor ID
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of invitations</returns>
    Task<List<Invitation>> GetByVisitorIdAsync(int visitorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invitations by host ID
    /// </summary>
    /// <param name="hostId">Host ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of invitations</returns>
    Task<List<Invitation>> GetByHostIdAsync(int hostId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invitations by status
    /// </summary>
    /// <param name="status">Invitation status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of invitations</returns>
    Task<List<Invitation>> GetByStatusAsync(InvitationStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending approvals
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending invitations</returns>
    Task<List<Invitation>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invitations by date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of invitations</returns>
    Task<List<Invitation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active invitations (checked in visitors)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active invitations</returns>
    Task<List<Invitation>> GetActiveInvitationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired invitations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expired invitations</returns>
    Task<List<Invitation>> GetExpiredInvitationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if invitation number exists
    /// </summary>
    /// <param name="invitationNumber">Invitation number</param>
    /// <param name="excludeId">ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists</returns>
    Task<bool> InvitationNumberExistsAsync(string invitationNumber, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invitation statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invitation statistics</returns>
    Task<InvitationStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invitation by invitation number
    /// </summary>
    /// <param name="invitationNumber">Invitation number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invitation if found</returns>
    Task<Invitation?> GetByInvitationNumberAsync(string invitationNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invitation by QR code
    /// </summary>
    /// <param name="qrCode">QR code data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invitation if found</returns>
    Task<Invitation?> GetByQrCodeAsync(string qrCode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Invitation statistics
/// </summary>
public class InvitationStatistics
{
    public int TotalInvitations { get; set; }
    public int PendingApprovals { get; set; }
    public int ApprovedInvitations { get; set; }
    public int ActiveVisitors { get; set; }
    public int CompletedVisits { get; set; }
    public int CancelledInvitations { get; set; }
    public int ExpiredInvitations { get; set; }
    public double AverageVisitDuration { get; set; }
    public Dictionary<InvitationStatus, int> StatusBreakdown { get; set; } = new();
}
