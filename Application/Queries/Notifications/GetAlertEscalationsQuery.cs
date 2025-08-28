using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Notifications;

namespace VisitorManagementSystem.Api.Application.Queries.Notifications;


/// <summary>
/// Query to get alert escalation rules
/// </summary>
public class GetAlertEscalationsQuery : IRequest<PagedResultDto<AlertEscalationDto>>
{
    /// <summary>
    /// Page index (0-based)
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Filter by alert type
    /// </summary>
    public string? AlertType { get; set; }

    /// <summary>
    /// Filter by priority
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// Filter by enabled status
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Search term for rule name
    /// </summary>
    public string? SearchTerm { get; set; }
}

/// <summary>
/// Query to get notification statistics
/// </summary>
public class GetNotificationStatsQuery : IRequest<NotificationStatsDto>
{
    /// <summary>
    /// User ID to get stats for (null for system-wide stats - admin only)
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Date range start for statistics
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Date range end for statistics
    /// </summary>
    public DateTime? ToDate { get; set; }
}
