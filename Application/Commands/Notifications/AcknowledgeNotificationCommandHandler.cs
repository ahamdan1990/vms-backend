using MediatR;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Notifications;

/// <summary>
/// Handler for acknowledging notification alerts
/// </summary>
public class AcknowledgeNotificationCommandHandler : IRequestHandler<AcknowledgeNotificationCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcknowledgeNotificationCommandHandler> _logger;

    public AcknowledgeNotificationCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<AcknowledgeNotificationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(AcknowledgeNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _unitOfWork.Repository<NotificationAlert>()
            .GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification == null)
            throw new KeyNotFoundException($"Notification with ID {request.NotificationId} not found");

        if (notification.IsAcknowledged)
        {
            _logger.LogWarning("Notification {NotificationId} already acknowledged", request.NotificationId);
            return true; // Already acknowledged, return success
        }

        var user = await _unitOfWork.Users.GetByIdAsync(request.AcknowledgedBy, cancellationToken)
                   ?? throw new KeyNotFoundException($"User with ID {request.AcknowledgedBy} not found");

        var canAcknowledge =
            request.CanAcknowledgeAll ||
            notification.TargetUserId == request.AcknowledgedBy ||
            (notification.TargetUserId == null && notification.TargetRole == null) ||
            (!string.IsNullOrWhiteSpace(notification.TargetRole) &&
             string.Equals(notification.TargetRole, user.Role.ToString(), StringComparison.OrdinalIgnoreCase));

        if (!canAcknowledge)
        {
            _logger.LogWarning(
                "User {UserId} attempted to acknowledge notification {NotificationId} without access",
                request.AcknowledgedBy,
                request.NotificationId);
            throw new UnauthorizedAccessException("You do not have access to acknowledge this notification");
        }

        // Acknowledge the notification
        notification.Acknowledge(request.AcknowledgedBy);

        // Notes are accepted by the API contract but are not persisted on NotificationAlert.
        // Keep the metadata payload intact so navigation and rendering continue to work.
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            _logger.LogInformation(
                "Acknowledgment notes were supplied for notification {NotificationId} by user {UserId} but were not persisted",
                request.NotificationId,
                request.AcknowledgedBy);
        }

        _unitOfWork.Repository<NotificationAlert>().Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification {NotificationId} acknowledged by user {UserId}", 
            request.NotificationId, request.AcknowledgedBy);

        return true;
    }
}
