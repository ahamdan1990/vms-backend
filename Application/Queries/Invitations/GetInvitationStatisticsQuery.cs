using MediatR;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Query to get invitation statistics
/// </summary>
public class GetInvitationStatisticsQuery : IRequest<InvitationStatistics>
{
    /// <summary>
    /// Optional date range filter - start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Optional date range filter - end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Optional host filter - only statistics for specific host
    /// </summary>
    public int? HostId { get; set; }

    /// <summary>
    /// Current user ID for access filtering
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// User permissions for determining access level
    /// </summary>
    public List<string> UserPermissions { get; set; } = new();

    /// <summary>
    /// Include deleted invitations in statistics
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}
