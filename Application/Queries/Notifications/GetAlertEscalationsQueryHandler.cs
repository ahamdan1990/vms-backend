using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Notifications;

/// <summary>
/// Handler for getting alert escalation rules
/// </summary>
public class GetAlertEscalationsQueryHandler : IRequestHandler<GetAlertEscalationsQuery, PagedResultDto<AlertEscalationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAlertEscalationsQueryHandler> _logger;

    public GetAlertEscalationsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAlertEscalationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResultDto<AlertEscalationDto>> Handle(GetAlertEscalationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _unitOfWork.Repository<AlertEscalation>().GetQueryable()
                .Include(e => e.EscalationTargetUser)
                .Include(e => e.Location)
                .Where(e => e.IsActive);

            // Apply filters
            if (!string.IsNullOrEmpty(request.AlertType) && Enum.TryParse<NotificationAlertType>(request.AlertType, out var alertType))
            {
                query = query.Where(e => e.AlertType == alertType);
            }

            if (!string.IsNullOrEmpty(request.Priority) && Enum.TryParse<AlertPriority>(request.Priority, out var priority))
            {
                query = query.Where(e => e.AlertPriority == priority);
            }

            if (request.IsEnabled.HasValue)
            {
                query = query.Where(e => e.IsEnabled == request.IsEnabled.Value);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                query = query.Where(e => e.RuleName.ToLower().Contains(searchLower));
            }

            // Order by rule priority, then by alert priority
            query = query.OrderBy(e => e.RulePriority).ThenBy(e => e.AlertPriority);

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var escalations = await query
                .Skip(request.PageIndex * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var escalationDtos = escalations.Select(MapToDto).ToList();

            return new PagedResultDto<AlertEscalationDto>
            {
                Items = escalationDtos,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alert escalations");
            throw;
        }
    }

    private static AlertEscalationDto MapToDto(AlertEscalation escalation)
    {
        return new AlertEscalationDto
        {
            Id = escalation.Id,
            RuleName = escalation.RuleName,
            AlertType = escalation.AlertType,
            AlertPriority = escalation.AlertPriority,
            TargetRole = escalation.TargetRole,
            LocationId = escalation.LocationId,
            LocationName = escalation.Location?.Name,
            EscalationDelayMinutes = escalation.EscalationDelayMinutes,
            Action = escalation.Action,
            EscalationTargetRole = escalation.EscalationTargetRole,
            EscalationTargetUserId = escalation.EscalationTargetUserId,
            EscalationTargetUserName = escalation.EscalationTargetUser?.FullName,
            EscalationEmails = escalation.EscalationEmails,
            EscalationPhones = escalation.EscalationPhones,
            MaxAttempts = escalation.MaxAttempts,
            IsEnabled = escalation.IsEnabled,
            RulePriority = escalation.RulePriority,
            Configuration = escalation.Configuration,
            CreatedOn = escalation.CreatedOn,
            ModifiedOn = escalation.ModifiedOn,
            IsActive = escalation.IsActive
        };
    }
}

/// <summary>
/// Handler for getting notification statistics
/// </summary>
public class GetNotificationStatsQueryHandler : IRequestHandler<GetNotificationStatsQuery, NotificationStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetNotificationStatsQueryHandler> _logger;

    public GetNotificationStatsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetNotificationStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<NotificationStatsDto> Handle(GetNotificationStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _unitOfWork.Repository<NotificationAlert>().GetQueryable()
                .Where(n => n.IsActive);

            // Filter by user if specified
            if (request.UserId.HasValue)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(request.UserId.Value, cancellationToken);
                if (user != null)
                {
                    query = query.Where(n => 
                        n.TargetUserId == request.UserId || 
                        n.TargetRole == user.Role.ToString() ||
                        (n.TargetUserId == null && n.TargetRole == null));
                }
            }

            // Apply date filters
            var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
            var toDate = request.ToDate ?? DateTime.UtcNow;

            query = query.Where(n => n.CreatedOn >= fromDate && n.CreatedOn <= toDate);

            var notifications = await query.ToListAsync(cancellationToken);

            var stats = new NotificationStatsDto
            {
                TotalUnacknowledged = notifications.Count(n => !n.IsAcknowledged),
                CriticalUnacknowledged = notifications.Count(n => !n.IsAcknowledged && n.Priority == AlertPriority.Critical),
                HighUnacknowledged = notifications.Count(n => !n.IsAcknowledged && n.Priority == AlertPriority.High),
                Last24Hours = notifications.Count(n => n.CreatedOn >= DateTime.UtcNow.AddHours(-24)),
                AlertTypeStats = notifications.GroupBy(n => n.Type.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),
                PriorityStats = notifications.GroupBy(n => n.Priority.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            stats.MostCommonType = stats.AlertTypeStats
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault().Key;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification statistics for user {UserId}", request.UserId);
            throw;
        }
    }
}
