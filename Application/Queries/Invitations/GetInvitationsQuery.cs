using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Query to get invitations with filtering and paging
/// </summary>
public class GetInvitationsQuery : IRequest<PagedResultDto<InvitationDto>>
{
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Search term (searches invitation number, visitor name, host name, subject)
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by invitation status
    /// </summary>
    public InvitationStatus? Status { get; set; }

    /// <summary>
    /// Filter by invitation type
    /// </summary>
    public InvitationType? Type { get; set; }

    /// <summary>
    /// Filter by host ID
    /// </summary>
    public int? HostId { get; set; }

    /// <summary>
    /// Filter by visitor ID
    /// </summary>
    public int? VisitorId { get; set; }

    /// <summary>
    /// Filter by visit purpose ID
    /// </summary>
    public int? VisitPurposeId { get; set; }

    /// <summary>
    /// Filter by location ID
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Filter by date range - start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by date range - end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Include deleted invitations
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>
    /// Only pending approvals
    /// </summary>
    public bool PendingApprovalsOnly { get; set; } = false;

    /// <summary>
    /// Only active invitations (checked in visitors)
    /// </summary>
    public bool ActiveOnly { get; set; } = false;

    /// <summary>
    /// Only expired invitations
    /// </summary>
    public bool ExpiredOnly { get; set; } = false;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "ScheduledStartTime";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}
