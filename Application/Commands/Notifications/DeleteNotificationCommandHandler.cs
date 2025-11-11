using MediatR;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Notifications;

/// <summary>
/// Handler for deleting notification alerts
/// </summary>
public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteNotificationCommandHandler> _logger;

    public DeleteNotificationCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteNotificationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _unitOfWork.Repository<NotificationAlert>()
            .GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification == null)
            throw new KeyNotFoundException($"Notification with ID {request.NotificationId} not found");

        // Soft delete the notification
        _unitOfWork.Repository<NotificationAlert>().Delete(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification {NotificationId} deleted by user {UserId}",
            request.NotificationId, request.DeletedBy);

        return true;
    }
}

/// <summary>
/// Handler for deleting all notifications for the current user
/// </summary>
public class DeleteAllNotificationsCommandHandler : IRequestHandler<DeleteAllNotificationsCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteAllNotificationsCommandHandler> _logger;

    public DeleteAllNotificationsCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteAllNotificationsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteAllNotificationsCommand request, CancellationToken cancellationToken)
    {
        // Get all notifications for the user (not yet deleted)
        var notifications = await _unitOfWork.Repository<NotificationAlert>()
            .GetAllAsync(cancellationToken);

        if (notifications == null || !notifications.Any())
        {
            _logger.LogInformation("No notifications found to delete for user {UserId}", request.DeletedBy);
            return true;
        }

        // Delete all notifications
        foreach (var notification in notifications)
        {
            _unitOfWork.Repository<NotificationAlert>().Delete(notification);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("All notifications ({Count}) deleted by user {UserId}",
            notifications.Count, request.DeletedBy);

        return true;
    }
}
