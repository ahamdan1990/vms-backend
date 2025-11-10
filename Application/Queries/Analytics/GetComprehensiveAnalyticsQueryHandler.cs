using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Analytics;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Analytics;

/// <summary>
/// Handler for getting comprehensive analytics - Simplified version using existing schema
/// </summary>
public class GetComprehensiveAnalyticsQueryHandler : IRequestHandler<GetComprehensiveAnalyticsQuery, ComprehensiveAnalyticsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetComprehensiveAnalyticsQueryHandler> _logger;

    public GetComprehensiveAnalyticsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetComprehensiveAnalyticsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ComprehensiveAnalyticsDto> Handle(GetComprehensiveAnalyticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var now = DateTime.UtcNow;

            var analytics = new ComprehensiveAnalyticsDto
            {
                TimeZone = request.TimeZone,
                GeneratedAt = now
            };

            // Get real-time metrics
            analytics.RealTimeMetrics = await GetRealTimeMetricsAsync(today, now, cancellationToken);

            // Get invitation metrics
            analytics.InvitationMetrics = await GetInvitationMetricsAsync(today, cancellationToken);

            // Get visitor metrics
            analytics.VisitorMetrics = await GetVisitorMetricsAsync(today, cancellationToken);

            // Set capacity metrics (simplified - using invitation-based logic)
            analytics.CapacityMetrics = await GetCapacityMetricsAsync(cancellationToken);

            // Set insights
            analytics.Insights = await GetInsightsAsync(today, cancellationToken);

            // Set trends (simplified)
            analytics.Trends = new TrendAnalyticsDto
            {
                PopularLocations = new List<PopularLocationDto>(),
                VisitPurposeTrends = new List<VisitPurposeTrendDto>()
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comprehensive analytics");
            throw;
        }
    }

    private async Task<RealTimeMetricsDto> GetRealTimeMetricsAsync(DateTime today, DateTime now, CancellationToken cancellationToken)
    {
        // Get today's invitations
        var todayInvitations = await _unitOfWork.Invitations.GetByDateRangeAsync(today, today.AddDays(1), cancellationToken);

        var expectedVisitorsToday = todayInvitations.Count(i => i.Status == InvitationStatus.Approved);
        var activeToday = todayInvitations.Count(i => i.Status == InvitationStatus.Active);

        // Get all active invitations (checked in)
        var allActiveInvitations = await _unitOfWork.Invitations.GetActiveInvitationsAsync(cancellationToken);
        var activeVisitorsInSystem = allActiveInvitations != null ? allActiveInvitations.Count : 0;

        return new RealTimeMetricsDto
        {
            ExpectedVisitorsToday = expectedVisitorsToday,
            CheckedInToday = activeToday,
            PendingCheckouts = activeVisitorsInSystem,
            WalkInsToday = 0, // Requires separate tracking
            ActiveVisitorsInSystem = activeVisitorsInSystem,
            TodayVisitors = activeToday,
            OverdueVisitors = 0, // Calculate based on ScheduledEndTime < now && Status == Active
            LastUpdated = now
        };
    }

    private async Task<CapacityMetricsDto> GetCapacityMetricsAsync(CancellationToken cancellationToken)
    {
        var locations = await _unitOfWork.Locations.GetActiveLocationsAsync(cancellationToken);
        var totalMaxCapacity = locations != null ? locations.Sum(l => l.MaxCapacity) : 0;

        var activeInvitations = await _unitOfWork.Invitations.GetActiveInvitationsAsync(cancellationToken);
        var currentOccupancy = activeInvitations != null ? activeInvitations.Count : 0;

        var utilizationRate = totalMaxCapacity > 0 ? (decimal)currentOccupancy / totalMaxCapacity * 100 : 0;

        return new CapacityMetricsDto
        {
            CurrentUtilization = utilizationRate,
            CurrentOccupancy = currentOccupancy,
            MaxCapacity = totalMaxCapacity,
            AvailableSlots = Math.Max(0, totalMaxCapacity - currentOccupancy),
            LocationBreakdown = new List<LocationCapacityDto>(),
            PeakHours = new List<PeakHourDto>()
        };
    }

    private async Task<VisitorMetricsDto> GetVisitorMetricsAsync(DateTime today, CancellationToken cancellationToken)
    {
        var totalVisitors = await _unitOfWork.Visitors.CountAsync(cancellationToken);

        return new VisitorMetricsDto
        {
            TotalVisitors = totalVisitors,
            ActiveToday = 0,
            CompletedToday = 0,
            AverageVisitDurationMinutes = 0,
            CheckInRate = 0,
            NoShowRate = 0,
            DailyTrend = new List<DailyVisitorTrendDto>()
        };
    }

    private async Task<InvitationMetricsDto> GetInvitationMetricsAsync(DateTime today, CancellationToken cancellationToken)
    {
        var totalInvitations = await _unitOfWork.Invitations.CountAsync(cancellationToken);
        var pendingInvitations = await _unitOfWork.Invitations.GetPendingApprovalsAsync(cancellationToken);

        var pendingCount = pendingInvitations != null ? pendingInvitations.Count : 0;

        return new InvitationMetricsDto
        {
            TotalInvitations = totalInvitations,
            PendingApproval = pendingCount,
            ApprovedToday = 0,
            RejectedToday = 0,
            ActiveToday = 0,
            CompletedToday = 0,
            ByStatus = new Dictionary<string, int>()
        };
    }

    private async Task<InsightsDto> GetInsightsAsync(DateTime today, CancellationToken cancellationToken)
    {
        var pendingInvitations = await _unitOfWork.Invitations.GetPendingApprovalsAsync(cancellationToken);

        return new InsightsDto
        {
            TodaysCheckIns = new List<CheckInActivityDto>(),
            PendingInvitations = pendingInvitations != null ? pendingInvitations.Count : 0,
            TotalApproved = 0,
            TotalActiveToday = 0,
            TotalRejected = 0,
            TotalCompleted = 0,
            Recommendations = new List<string>
            {
                "Analytics system active - gathering baseline data"
            },
            RecentAlerts = new List<AlertSummaryDto>()
        };
    }
}
