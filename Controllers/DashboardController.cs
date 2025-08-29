using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Queries.Visitors;
using VisitorManagementSystem.Api.Application.Queries.Invitations;
using VisitorManagementSystem.Api.Controllers;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Dashboard controller for aggregated metrics and real-time data
/// Provides unified dashboard data to reduce frontend API calls
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ILogger<DashboardController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get aggregated dashboard metrics
    /// Combines visitor, invitation, and system data in a single response
    /// </summary>
    /// <returns>Comprehensive dashboard metrics</returns>
    [HttpGet("metrics")]
    [Authorize(Policy = Permissions.Dashboard.ViewMetrics)]
    public async Task<IActionResult> GetDashboardMetrics(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching aggregated dashboard metrics");

            // Get today's date range
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);

            // Fetch data from existing query handlers
            var visitorStats = await _mediator.Send(new GetVisitorStatsQuery(), cancellationToken);
            var invitationStats = await _mediator.Send(new GetInvitationStatisticsQuery(), cancellationToken);
            var todayInvitationStats = await _mediator.Send(new GetInvitationStatisticsQuery 
            { 
                StartDate = todayStart, 
                EndDate = todayEnd 
            }, cancellationToken);

            // Get system alerts count
            var systemAlertsCount = await GetSystemAlertsCount(cancellationToken);

            // Calculate overdue visitors
            var overdueVisitors = await GetOverdueVisitorsCount(cancellationToken);

            var metrics = new DashboardMetricsDto
            {
                TodayVisitors = todayInvitationStats.TotalInvitations,
                ActiveVisitors = invitationStats.ActiveVisitors,
                PendingInvitations = invitationStats.PendingApprovals,
                SystemAlerts = systemAlertsCount,
                OverdueVisitors = overdueVisitors,
                LastUpdated = DateTime.UtcNow,
                
                // Additional context data
                TotalVisitors = visitorStats.TotalVisitors,
                CompletedVisitsToday = todayInvitationStats.CompletedVisits,
                AverageVisitDuration = invitationStats.AverageVisitDuration
            };

            _logger.LogDebug("Dashboard metrics generated: Today={TodayVisitors}, Active={ActiveVisitors}, Pending={PendingInvitations}",
                metrics.TodayVisitors, metrics.ActiveVisitors, metrics.PendingInvitations);

            return SuccessResponse(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard metrics");
            return BadRequestResponse("Failed to generate dashboard metrics", ex.Message);
        }
    }

    /// <summary>
    /// Get system alerts count (unacknowledged alerts from last 24 hours)
    /// </summary>
    private async Task<int> GetSystemAlertsCount(CancellationToken cancellationToken)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            var alerts = await _unitOfWork.Repository<NotificationAlert>()
                .GetAllAsync(
                    alert => !alert.IsAcknowledged && 
                            alert.CreatedOn >= cutoffTime,
                    cancellationToken: cancellationToken);
            
            return alerts.Count();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get system alerts count");
            return 0;
        }
    }

    /// <summary>
    /// Get count of visitors who have overstayed their scheduled time
    /// </summary>
    private async Task<int> GetOverdueVisitorsCount(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var overdueInvitations = await _unitOfWork.Invitations.GetAsync(
                invitation => invitation.Status == Domain.Enums.InvitationStatus.Active &&
                             invitation.CheckedInAt.HasValue &&
                             !invitation.CheckedOutAt.HasValue &&
                             invitation.ScheduledEndTime < now,
                cancellationToken: cancellationToken);

            return overdueInvitations.Count();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get overdue visitors count");
            return 0;
        }
    }
}

/// <summary>
/// Dashboard metrics data transfer object
/// </summary>
public class DashboardMetricsDto
{
    /// <summary>
    /// Number of visitors who visited today
    /// </summary>
    public int TodayVisitors { get; set; }

    /// <summary>
    /// Number of currently active visitors (checked in but not out)
    /// </summary>
    public int ActiveVisitors { get; set; }

    /// <summary>
    /// Number of invitations pending approval
    /// </summary>
    public int PendingInvitations { get; set; }

    /// <summary>
    /// Number of unacknowledged system alerts
    /// </summary>
    public int SystemAlerts { get; set; }

    /// <summary>
    /// Number of visitors who have overstayed their scheduled time
    /// </summary>
    public int OverdueVisitors { get; set; }

    /// <summary>
    /// When these metrics were last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Total number of visitors in system
    /// </summary>
    public int TotalVisitors { get; set; }

    /// <summary>
    /// Number of completed visits today
    /// </summary>
    public int CompletedVisitsToday { get; set; }

    /// <summary>
    /// Average visit duration in hours
    /// </summary>
    public double AverageVisitDuration { get; set; }
}