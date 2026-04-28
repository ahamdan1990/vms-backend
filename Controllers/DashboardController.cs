using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Queries.Visitors;
using VisitorManagementSystem.Api.Application.Queries.Invitations;
using VisitorManagementSystem.Api.Application.Services.Common;
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
    private readonly ISystemTimeZoneService _systemTimeZoneService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ISystemTimeZoneService systemTimeZoneService,
        ILogger<DashboardController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _systemTimeZoneService = systemTimeZoneService ?? throw new ArgumentNullException(nameof(systemTimeZoneService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get aggregated dashboard metrics
    /// Combines visitor, invitation, and system data in a single response
    /// </summary>
    /// <returns>Comprehensive dashboard metrics</returns>
    [HttpGet("metrics")]
    [Authorize(Policy = Permissions.Dashboard.ViewOperations)]
    public async Task<IActionResult> GetDashboardMetrics(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching aggregated dashboard metrics");

            var systemTimeZone = await _systemTimeZoneService.GetSystemTimeZoneAsync(cancellationToken);
            var systemToday = DateTime.SpecifyKind(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, systemTimeZone).Date,
                DateTimeKind.Unspecified);

            // Get current user context for filtering
            var userId = GetCurrentUserId();
            var userPermissions = GetCurrentUserPermissions();

            // Fetch data from existing query handlers with user context
            var visitorStats = await _mediator.Send(new GetVisitorStatsQuery
            {
                UserId = userId,
                UserPermissions = userPermissions
            }, cancellationToken);

            var invitationStats = await _mediator.Send(new GetInvitationStatisticsQuery
            {
                UserId = userId,
                UserPermissions = userPermissions
            }, cancellationToken);

            var todayInvitationStats = await _mediator.Send(new GetInvitationStatisticsQuery
            {
                StartDate = systemToday,
                EndDate = systemToday,
                UserId = userId,
                UserPermissions = userPermissions
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
    /// Get visitors who both checked in and checked out during the current system day.
    /// </summary>
    [HttpGet("receptionist/completed-today")]
    [Authorize(Policy = Permissions.Dashboard.ViewOperations)]
    public async Task<IActionResult> GetCompletedTodayVisitors([FromQuery] int? locationId, CancellationToken cancellationToken)
    {
        try
        {
            var visitors = await _mediator.Send(new GetCompletedTodayInvitationsQuery
            {
                LocationId = locationId
            }, cancellationToken);

            return SuccessResponse(visitors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving completed-today visitors for dashboard");
            return BadRequestResponse("Failed to retrieve completed-today visitors", ex.Message);
        }
    }

    /// <summary>
    /// Get all visitors currently in the building (checked in, not yet checked out).
    /// Administrators only — used by the manager live dashboard.
    /// </summary>
    [HttpGet("in-building")]
    [Authorize(Policy = Permissions.Dashboard.ViewAdmin)]
    public async Task<IActionResult> GetInBuildingVisitors(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var activeInvitations = await _unitOfWork.Invitations.GetActiveInvitationsAsync(cancellationToken);

            var result = activeInvitations.Select(inv => new InBuildingVisitorDto
            {
                InvitationId = inv.Id,
                VisitorId = inv.VisitorId,
                VisitorName = $"{inv.Visitor.FirstName} {inv.Visitor.LastName}",
                Company = inv.Visitor.Company ?? inv.Visitor.CompanyEntity?.Name,
                ProfilePhotoPath = inv.Visitor.ProfilePhotoPath,
                HostName = $"{inv.Host.FirstName} {inv.Host.LastName}",
                LocationName = inv.Location?.Name,
                VisitPurpose = inv.VisitPurpose?.Name,
                CheckedInAt = inv.CheckedInAt ?? inv.CreatedOn,
                ScheduledEndTime = inv.ScheduledEndTime,
                IsVip = inv.Visitor.IsVip,
                IsBlacklisted = inv.Visitor.IsBlacklisted,
                IsOverdue = inv.ScheduledEndTime < now,
                ElapsedMinutes = (int)(now - (inv.CheckedInAt ?? inv.CreatedOn)).TotalMinutes
            }).OrderBy(v => v.CheckedInAt).ToList();

            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving in-building visitors");
            return BadRequestResponse("Failed to retrieve in-building visitors", ex.Message);
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

public class InBuildingVisitorDto
{
    public int InvitationId { get; set; }
    public int VisitorId { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? ProfilePhotoPath { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string? LocationName { get; set; }
    public string? VisitPurpose { get; set; }
    public DateTime CheckedInAt { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public bool IsVip { get; set; }
    public bool IsBlacklisted { get; set; }
    public bool IsOverdue { get; set; }
    public int ElapsedMinutes { get; set; }
}
