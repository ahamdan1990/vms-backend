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

        // Acknowledge the notification
        notification.Acknowledge(request.AcknowledgedBy);

        // Add optional notes to audit data
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            notification.PayloadData = request.Notes;
        }

        _unitOfWork.Repository<NotificationAlert>().Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification {NotificationId} acknowledged by user {UserId}", 
            request.NotificationId, request.AcknowledgedBy);

        return true;
    }
}
