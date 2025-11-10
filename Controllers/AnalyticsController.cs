using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VisitorManagementSystem.Api.Application.DTOs.Analytics;
using VisitorManagementSystem.Api.Application.Queries.Analytics;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Hubs;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for analytics and dashboard metrics
/// </summary>
[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IHubContext<AdminHub> _adminHubContext;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IMediator mediator,
        IHubContext<AdminHub> adminHubContext,
        ILogger<AnalyticsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _adminHubContext = adminHubContext ?? throw new ArgumentNullException(nameof(adminHubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get comprehensive analytics for dashboard
    /// </summary>
    /// <param name="startDate">Optional start date for trend data</param>
    /// <param name="endDate">Optional end date for trend data</param>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="timeZone">Time zone for date/time display (default: UTC)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive analytics data</returns>
    [HttpGet("comprehensive")]
    [ProducesResponseType(typeof(ComprehensiveAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetComprehensiveAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? locationId,
        [FromQuery] string? timeZone,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                LocationId = locationId,
                TimeZone = timeZone ?? "UTC"
            };

            var analytics = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comprehensive analytics");
            return ServerErrorResponse("Failed to retrieve analytics data");
        }
    }

    /// <summary>
    /// Get real-time metrics for dashboard overview
    /// </summary>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Real-time metrics</returns>
    [HttpGet("realtime")]
    [ProducesResponseType(typeof(RealTimeMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRealTimeMetrics(
        [FromQuery] int? locationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery
            {
                LocationId = locationId
            };

            var analytics = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(analytics.RealTimeMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real-time metrics");
            return ServerErrorResponse("Failed to retrieve real-time metrics");
        }
    }

    /// <summary>
    /// Get capacity metrics
    /// </summary>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Capacity metrics</returns>
    [HttpGet("capacity")]
    [ProducesResponseType(typeof(CapacityMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCapacityMetrics(
        [FromQuery] int? locationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery
            {
                LocationId = locationId
            };

            var analytics = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(analytics.CapacityMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting capacity metrics");
            return ServerErrorResponse("Failed to retrieve capacity metrics");
        }
    }

    /// <summary>
    /// Get visitor metrics
    /// </summary>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Visitor metrics</returns>
    [HttpGet("visitors")]
    [ProducesResponseType(typeof(VisitorMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVisitorMetrics(
        [FromQuery] int? locationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery
            {
                LocationId = locationId
            };

            var analytics = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(analytics.VisitorMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visitor metrics");
            return ServerErrorResponse("Failed to retrieve visitor metrics");
        }
    }

    /// <summary>
    /// Get invitation metrics
    /// </summary>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invitation metrics</returns>
    [HttpGet("invitations")]
    [ProducesResponseType(typeof(InvitationMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvitationMetrics(
        [FromQuery] int? locationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery
            {
                LocationId = locationId
            };

            var analytics = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(analytics.InvitationMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invitation metrics");
            return ServerErrorResponse("Failed to retrieve invitation metrics");
        }
    }

    /// <summary>
    /// Get trend analytics
    /// </summary>
    /// <param name="startDate">Start date for trends</param>
    /// <param name="endDate">End date for trends</param>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trend analytics</returns>
    [HttpGet("trends")]
    [ProducesResponseType(typeof(TrendAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrendAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? locationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                LocationId = locationId
            };

            var analytics = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(analytics.Trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trend analytics");
            return ServerErrorResponse("Failed to retrieve trend analytics");
        }
    }

    /// <summary>
    /// Get insights and recommendations
    /// </summary>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Insights and recommendations</returns>
    [HttpGet("insights")]
    [ProducesResponseType(typeof(InsightsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInsights(
        [FromQuery] int? locationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery
            {
                LocationId = locationId
            };

            var analytics = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(analytics.Insights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting insights");
            return ServerErrorResponse("Failed to retrieve insights");
        }
    }

    /// <summary>
    /// Get peak hours for today or specified date
    /// </summary>
    /// <param name="date">Optional date (default: today)</param>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Peak hours data</returns>
    [HttpGet("peak-hours")]
    [ProducesResponseType(typeof(List<PeakHourDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPeakHours(
        [FromQuery] DateTime? date,
        [FromQuery] int? locationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery
            {
                LocationId = locationId
            };

            var analytics = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(analytics.CapacityMetrics.PeakHours);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting peak hours");
            return ServerErrorResponse("Failed to retrieve peak hours");
        }
    }

    /// <summary>
    /// Get popular locations ranking
    /// </summary>
    /// <param name="startDate">Start date for analysis</param>
    /// <param name="endDate">End date for analysis</param>
    /// <param name="top">Number of top locations to return (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Popular locations</returns>
    [HttpGet("popular-locations")]
    [ProducesResponseType(typeof(List<PopularLocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPopularLocations(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            var analytics = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(analytics.Trends.PopularLocations.Take(top));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular locations");
            return ServerErrorResponse("Failed to retrieve popular locations");
        }
    }

    /// <summary>
    /// Get daily visitor trend
    /// </summary>
    /// <param name="days">Number of days to include (default: 30)</param>
    /// <param name="locationId">Optional location filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Daily visitor trend</returns>
    [HttpGet("daily-trend")]
    [ProducesResponseType(typeof(List<DailyVisitorTrendDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailyTrend(
        [FromQuery] int days = 30,
        [FromQuery] int? locationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery
            {
                StartDate = DateTime.UtcNow.Date.AddDays(-days),
                EndDate = DateTime.UtcNow,
                LocationId = locationId
            };

            var analytics = await _mediator.Send(query, cancellationToken);
            return SuccessResponse(analytics.VisitorMetrics.DailyTrend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily trend");
            return ServerErrorResponse("Failed to retrieve daily trend");
        }
    }

    /// <summary>
    /// Broadcast analytics update to connected clients (Admin only)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("broadcast-update")]
    [Authorize(Roles = UserRoles.Administrator)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BroadcastAnalyticsUpdate(CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery();
            var analytics = await _mediator.Send(query, cancellationToken);

            // Broadcast to all connected clients
            await _adminHubContext.Clients.All.SendAsync("DashboardMetricsUpdated", analytics, cancellationToken);

            _logger.LogInformation("Analytics broadcast triggered by user {UserId}", GetCurrentUserId());

            return SuccessResponse(new { Message = "Analytics update broadcast successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting analytics update");
            return ServerErrorResponse("Failed to broadcast analytics update");
        }
    }

    /// <summary>
    /// Get analytics summary for export
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="format">Export format (json, csv) - default: json</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analytics data in requested format</returns>
    [HttpGet("export")]
    [Authorize(Roles = UserRoles.Administrator)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string format = "json",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetComprehensiveAnalyticsQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            var analytics = await _mediator.Send(query, cancellationToken);

            if (format.ToLower() == "csv")
            {
                // TODO: Implement CSV export
                return BadRequestResponse("CSV export not yet implemented");
            }

            // Return JSON by default
            return SuccessResponse(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting analytics");
            return ServerErrorResponse("Failed to export analytics");
        }
    }
}
