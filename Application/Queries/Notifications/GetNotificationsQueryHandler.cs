using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Notifications;

/// <summary>
/// Handler for getting paginated notifications
/// </summary>
public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, PagedResultDto<NotificationAlertDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetNotificationsQueryHandler> _logger;

    public GetNotificationsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetNotificationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResultDto<NotificationAlertDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _unitOfWork.Repository<NotificationAlert>().GetQueryable()
                .Include(n => n.AcknowledgedByUser)
                .Include(n => n.TargetLocation)
                .Where(n => n.IsActive);

            // Filter by user (either specific user or their role)
            if (request.UserId.HasValue)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId.Value, cancellationToken);
                if (user != null)
                {
                    query = query.Where(n => 
                        n.TargetUserId == request.UserId || 
                        n.TargetRole == user.Role.ToString() ||
                        (n.TargetUserId == null && n.TargetRole == null)); // Global notifications
                }
            }

            // Apply filters
            if (request.IsAcknowledged.HasValue)
            {
                query = query.Where(n => n.IsAcknowledged == request.IsAcknowledged.Value);
            }

            if (!string.IsNullOrEmpty(request.AlertType) && Enum.TryParse<NotificationAlertType>(request.AlertType, out var alertType))
            {
                query = query.Where(n => n.Type == alertType);
            }

            if (!string.IsNullOrEmpty(request.Priority) && Enum.TryParse<AlertPriority>(request.Priority, out var priority))
            {
                query = query.Where(n => n.Priority == priority);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(n => n.CreatedOn >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(n => n.CreatedOn <= request.ToDate.Value);
            }

            if (!request.IncludeExpired)
            {
                query = query.Where(n => n.ExpiresOn == null || n.ExpiresOn > DateTime.UtcNow);
            }

            // Order by priority first, then by creation date (newest first)
            query = query.OrderBy(n => n.Priority).ThenByDescending(n => n.CreatedOn);

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var notifications = await query
                .Skip(request.PageIndex * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var notificationDtos = notifications.Select(MapToDto).ToList();

            return new PagedResultDto<NotificationAlertDto>
            {
                Items = notificationDtos,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", request.UserId);
            throw;
        }
    }

    private static NotificationAlertDto MapToDto(NotificationAlert notification)
    {
        return new NotificationAlertDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Priority = notification.Priority,
            TargetRole = notification.TargetRole,
            TargetUserId = notification.TargetUserId,
            TargetLocationId = notification.TargetLocationId,
            LocationName = notification.TargetLocation?.Name,
            IsAcknowledged = notification.IsAcknowledged,
            AcknowledgedBy = notification.AcknowledgedBy,
            AcknowledgedByName = notification.AcknowledgedByUser?.FullName,
            AcknowledgedOn = notification.AcknowledgedOn,
            SentExternally = notification.SentExternally,
            SentExternallyOn = notification.SentExternallyOn,
            RelatedEntityType = notification.RelatedEntityType,
            RelatedEntityId = notification.RelatedEntityId,
            PayloadData = notification.PayloadData,
            ExpiresOn = notification.ExpiresOn,
            CreatedOn = notification.CreatedOn,
            ModifiedOn = notification.ModifiedOn,
            IsActive = notification.IsActive
        };
    }
}
