using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Notifications;

namespace VisitorManagementSystem.Api.Application.Queries.Notifications;

/// <summary>
/// Query to get paginated notifications for a user
/// </summary>
public class GetNotificationsQuery : IRequest<PagedResultDto<NotificationAlertDto>>
{
    /// <summary>
    /// User ID to get notifications for (null for all notifications - admin only)
    /// </summary>
    public int? UserId { get; init; }

    /// <summary>
    /// Page index (0-based)
    /// </summary>
    public int PageIndex { get; init; } = 0;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Filter by acknowledged status
    /// </summary>
    public bool? IsAcknowledged { get; init; }

    /// <summary>
    /// Filter by alert type
    /// </summary>
    public string? AlertType { get; init; }

    /// <summary>
    /// Filter by priority
    /// </summary>
    public string? Priority { get; init; }

    /// <summary>
    /// Filter by date range start
    /// </summary>
    public DateTime? FromDate { get; init; }

    /// <summary>
    /// Filter by date range end
    /// </summary>
    public DateTime? ToDate { get; init; }

    /// <summary>
    /// Include expired notifications
    /// </summary>
    public bool IncludeExpired { get; init; } = false;
}
