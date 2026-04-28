using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.Notifications;
using VisitorManagementSystem.Api.Application.DTOs.Notifications;
using VisitorManagementSystem.Api.Application.Queries.Notifications;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Controllers.Admin;

/// <summary>
/// Manages which users or roles receive specific alert types.
/// </summary>
[ApiController]
[Route("api/admin/alert-recipients")]
[Authorize(Policy = Permissions.Notification.ManageRecipients)]
public class AlertRecipientsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<AlertRecipientsController> _logger;

    public AlertRecipientsController(IMediator mediator, ILogger<AlertRecipientsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// List all configured alert recipients, optionally filtered by alert type.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] NotificationAlertType? alertType = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAlertRecipientsQuery(alertType), cancellationToken);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Add a new recipient rule (role-based or user-specific) for an alert type.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAlertRecipientDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return UnauthorizedResponse();

        if (dto.TargetType == "Role" && string.IsNullOrWhiteSpace(dto.TargetRole))
            return BadRequestResponse("TargetRole is required when TargetType is 'Role'.");

        if (dto.TargetType == "User" && dto.TargetUserId == null)
            return BadRequestResponse("TargetUserId is required when TargetType is 'User'.");

        if (dto.TargetType != "Role" && dto.TargetType != "User")
            return BadRequestResponse("TargetType must be 'Role' or 'User'.");

        var command = new CreateAlertRecipientCommand(
            dto.AlertType,
            dto.TargetType,
            dto.TargetRole,
            dto.TargetUserId,
            dto.Description,
            userId.Value);

        var result = await _mediator.Send(command, cancellationToken);
        _logger.LogInformation(
            "Alert recipient created: AlertType={AlertType}, TargetType={TargetType} by user {UserId}",
            dto.AlertType, dto.TargetType, userId);

        return CreatedResponse(result);
    }

    /// <summary>
    /// Enable/disable a recipient rule or update its description.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateAlertRecipientDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return UnauthorizedResponse();

        var command = new UpdateAlertRecipientCommand(id, dto.IsEnabled, dto.Description, userId.Value);
        var result = await _mediator.Send(command, cancellationToken);

        if (result == null) return NotFoundResponse("AlertRecipientConfiguration", id);

        return SuccessResponse(result);
    }

    /// <summary>
    /// Remove a recipient rule (soft-delete).
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return UnauthorizedResponse();

        var command = new DeleteAlertRecipientCommand(id, userId.Value);
        var deleted = await _mediator.Send(command, cancellationToken);

        if (!deleted) return NotFoundResponse("AlertRecipientConfiguration", id);

        _logger.LogInformation("Alert recipient {Id} deleted by user {UserId}", id, userId);
        return SuccessResponse("Recipient configuration deleted.");
    }
}
