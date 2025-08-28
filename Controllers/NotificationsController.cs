using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using VisitorManagementSystem.Api.Application.Commands.Notifications;
using VisitorManagementSystem.Api.Application.Queries.Notifications;
using VisitorManagementSystem.Api.Application.DTOs.Notifications;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Notification management controller for handling alerts, acknowledgments, and escalation rules
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(IMediator mediator, ILogger<NotificationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Gets paginated notifications for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResultDto<NotificationAlertDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 401)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isAcknowledged = null,
        [FromQuery] string? alertType = null,
        [FromQuery] string? priority = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool includeExpired = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return UnauthorizedResponse("User not authenticated");

            var query = new GetNotificationsQuery
            {
                UserId = userId.Value,
                PageIndex = pageIndex,
                PageSize = Math.Min(pageSize, 100),
                IsAcknowledged = isAcknowledged,
                AlertType = alertType,
                Priority = priority,
                FromDate = fromDate,
                ToDate = toDate,
                IncludeExpired = includeExpired
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", GetCurrentUserId());
            return ServerErrorResponse("An error occurred while retrieving notifications");
        }
    }

    /// <summary>
    /// Gets all notifications for administrators
    /// </summary>
    [HttpGet("all")]
    [Authorize(Policy = Permissions.Notification.ReadAll)]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResultDto<NotificationAlertDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> GetAllNotifications(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? userId = null,
        [FromQuery] bool? isAcknowledged = null,
        [FromQuery] string? alertType = null,
        [FromQuery] string? priority = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool includeExpired = false)
    {
        try
        {
            var query = new GetNotificationsQuery
            {
                UserId = userId,
                PageIndex = pageIndex,
                PageSize = Math.Min(pageSize, 100),
                IsAcknowledged = isAcknowledged,
                AlertType = alertType,
                Priority = priority,
                FromDate = fromDate,
                ToDate = toDate,
                IncludeExpired = includeExpired
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all notifications");
            return ServerErrorResponse("An error occurred while retrieving notifications");
        }
    }

    /// <summary>
    /// Acknowledges a notification
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    public async Task<IActionResult> AcknowledgeNotification(
        int id, 
        [FromBody] AcknowledgeNotificationDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return UnauthorizedResponse("User not authenticated");

            var command = new AcknowledgeNotificationCommand
            {
                NotificationId = id,
                Notes = request.Notes,
                AcknowledgedBy = userId.Value
            };

            var result = await _mediator.Send(command);
            return SuccessResponse(result, "Notification acknowledged successfully");
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse("Notification", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging notification {NotificationId}", id);
            return ServerErrorResponse("An error occurred while acknowledging notification");
        }
    }

    /// <summary>
    /// Gets notification statistics for the current user
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponseDto<NotificationStatsDto>), 200)]
    public async Task<IActionResult> GetNotificationStats(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return UnauthorizedResponse("User not authenticated");

            var query = new GetNotificationStatsQuery
            {
                UserId = userId.Value,
                FromDate = fromDate,
                ToDate = toDate
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification stats for user {UserId}", GetCurrentUserId());
            return ServerErrorResponse("An error occurred while retrieving notification statistics");
        }
    }

    /// <summary>
    /// Gets system-wide notification statistics for administrators
    /// </summary>
    [HttpGet("stats/system")]
    [Authorize(Policy = Permissions.Notification.ReadAll)]
    [ProducesResponseType(typeof(ApiResponseDto<NotificationStatsDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> GetSystemNotificationStats(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var query = new GetNotificationStatsQuery
            {
                UserId = null, // System-wide stats
                FromDate = fromDate,
                ToDate = toDate
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system notification stats");
            return ServerErrorResponse("An error occurred while retrieving system notification statistics");
        }
    }

    /// <summary>
    /// Gets alert escalation rules for administrators
    /// </summary>
    [HttpGet("escalations")]
    [Authorize(Policy = Permissions.Configuration.Read)]
    [ProducesResponseType(typeof(ApiResponseDto<PagedResultDto<AlertEscalationDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> GetAlertEscalations(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? alertType = null,
        [FromQuery] string? priority = null,
        [FromQuery] bool? isEnabled = null,
        [FromQuery] string? searchTerm = null)
    {
        try
        {
            var query = new GetAlertEscalationsQuery
            {
                PageIndex = pageIndex,
                PageSize = Math.Min(pageSize, 100),
                AlertType = alertType,
                Priority = priority,
                IsEnabled = isEnabled,
                SearchTerm = searchTerm
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert escalations");
            return ServerErrorResponse("An error occurred while retrieving alert escalations");
        }
    }

    /// <summary>
    /// Creates a new alert escalation rule
    /// </summary>
    [HttpPost("escalations")]
    [Authorize(Policy = Permissions.Configuration.Create)]
    [ProducesResponseType(typeof(ApiResponseDto<AlertEscalationDto>), 201)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> CreateAlertEscalation([FromBody] CreateUpdateAlertEscalationDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return UnauthorizedResponse("User not authenticated");

            var command = new CreateAlertEscalationCommand
            {
                RuleName = request.RuleName,
                AlertType = request.AlertType,
                AlertPriority = request.AlertPriority,
                TargetRole = request.TargetRole,
                LocationId = request.LocationId,
                EscalationDelayMinutes = request.EscalationDelayMinutes,
                Action = request.Action,
                EscalationTargetRole = request.EscalationTargetRole,
                EscalationTargetUserId = request.EscalationTargetUserId,
                EscalationEmails = request.EscalationEmails,
                EscalationPhones = request.EscalationPhones,
                MaxAttempts = request.MaxAttempts,
                IsEnabled = request.IsEnabled,
                RulePriority = request.RulePriority,
                Configuration = request.Configuration,
                CreatedBy = userId.Value
            };

            var result = await _mediator.Send(command);
            return CreatedResponse(result, null, "Alert escalation rule created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert escalation rule");
            return ServerErrorResponse("An error occurred while creating alert escalation rule");
        }
    }

    /// <summary>
    /// Updates an existing alert escalation rule
    /// </summary>
    [HttpPut("escalations/{id}")]
    [Authorize(Policy = Permissions.Configuration.Update)]
    [ProducesResponseType(typeof(ApiResponseDto<AlertEscalationDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 400)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> UpdateAlertEscalation(int id, [FromBody] CreateUpdateAlertEscalationDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return UnauthorizedResponse("User not authenticated");

            var command = new UpdateAlertEscalationCommand
            {
                Id = id,
                RuleName = request.RuleName,
                TargetRole = request.TargetRole,
                LocationId = request.LocationId,
                EscalationDelayMinutes = request.EscalationDelayMinutes,
                Action = request.Action,
                EscalationTargetRole = request.EscalationTargetRole,
                EscalationTargetUserId = request.EscalationTargetUserId,
                EscalationEmails = request.EscalationEmails,
                EscalationPhones = request.EscalationPhones,
                MaxAttempts = request.MaxAttempts,
                IsEnabled = request.IsEnabled,
                RulePriority = request.RulePriority,
                Configuration = request.Configuration,
                ModifiedBy = userId.Value
            };

            var result = await _mediator.Send(command);
            return SuccessResponse(result, "Alert escalation rule updated successfully");
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse("Alert escalation rule", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert escalation rule {Id}", id);
            return ServerErrorResponse("An error occurred while updating alert escalation rule");
        }
    }

    /// <summary>
    /// Deletes an alert escalation rule
    /// </summary>
    [HttpDelete("escalations/{id}")]
    [Authorize(Policy = Permissions.Configuration.Delete)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 404)]
    [ProducesResponseType(typeof(ApiResponseDto<object>), 403)]
    public async Task<IActionResult> DeleteAlertEscalation(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return UnauthorizedResponse("User not authenticated");

            var command = new DeleteAlertEscalationCommand
            {
                Id = id,
                DeletedBy = userId.Value
            };

            var result = await _mediator.Send(command);
            return SuccessResponse(result, "Alert escalation rule deleted successfully");
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResponse("Alert escalation rule", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting alert escalation rule {Id}", id);
            return ServerErrorResponse("An error occurred while deleting alert escalation rule");
        }
    }

    /// <summary>
    /// Gets available notification alert types
    /// </summary>
    [HttpGet("alert-types")]
    [ProducesResponseType(typeof(ApiResponseDto<Dictionary<string, string>>), 200)]
    public IActionResult GetAlertTypes()
    {
        try
        {
            var alertTypes = Enum.GetValues<NotificationAlertType>()
                .ToDictionary(x => x.ToString(), x => x.ToString());

            return SuccessResponse(alertTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert types");
            return ServerErrorResponse("An error occurred while retrieving alert types");
        }
    }

    /// <summary>
    /// Gets available alert priorities
    /// </summary>
    [HttpGet("priorities")]
    [ProducesResponseType(typeof(ApiResponseDto<Dictionary<string, string>>), 200)]
    public IActionResult GetAlertPriorities()
    {
        try
        {
            var priorities = Enum.GetValues<AlertPriority>()
                .ToDictionary(x => x.ToString(), x => x.ToString());

            return SuccessResponse(priorities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert priorities");
            return ServerErrorResponse("An error occurred while retrieving alert priorities");
        }
    }

    /// <summary>
    /// Gets available escalation actions
    /// </summary>
    [HttpGet("escalation-actions")]
    [Authorize(Policy = Permissions.Configuration.Read)]
    [ProducesResponseType(typeof(ApiResponseDto<Dictionary<string, string>>), 200)]
    public IActionResult GetEscalationActions()
    {
        try
        {
            var actions = Enum.GetValues<EscalationAction>()
                .ToDictionary(x => x.ToString(), x => x.ToString());

            return SuccessResponse(actions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting escalation actions");
            return ServerErrorResponse("An error occurred while retrieving escalation actions");
        }
    }
}
