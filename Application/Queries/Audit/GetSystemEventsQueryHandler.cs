using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Audit;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Audit;

/// <summary>
/// Handler for getting system events
/// </summary>
public class GetSystemEventsQueryHandler : IRequestHandler<GetSystemEventsQuery, PagedResultDto<AuditLogDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetSystemEventsQueryHandler> _logger;

    public GetSystemEventsQueryHandler(IUnitOfWork unitOfWork, ILogger<GetSystemEventsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResultDto<AuditLogDto>> Handle(GetSystemEventsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting system events. Page: {PageIndex}, Size: {PageSize}", 
                request.PageIndex, request.PageSize);

            var auditLogs = await _unitOfWork.AuditLogs.GetAllAsync(cancellationToken);

            // Filter for system events - events that represent system operations
            var filteredLogs = auditLogs.AsQueryable()
                .Where(a => a.EventType == EventType.SystemMaintenance || 
                           a.EventType == EventType.SystemConfiguration ||
                           a.EventType == EventType.Performance ||
                           a.EventType == EventType.Error);

            if (request.DateFrom.HasValue)
            {
                filteredLogs = filteredLogs.Where(a => a.CreatedOn >= request.DateFrom.Value);
            }

            if (request.DateTo.HasValue)
            {
                filteredLogs = filteredLogs.Where(a => a.CreatedOn <= request.DateTo.Value);
            }

            if (!string.IsNullOrEmpty(request.Severity))
            {
                filteredLogs = filteredLogs.Where(a => a.RiskLevel == request.Severity);
            }

            // Apply ordering
            var orderedLogs = filteredLogs.OrderByDescending(a => a.CreatedOn);

            // Apply pagination
            var totalCount = orderedLogs.Count();
            var pagedLogs = orderedLogs
                .Skip(request.PageIndex * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Map to DTOs
            var auditLogDtos = pagedLogs.Select(a => new AuditLogDto
            {
                Id = a.Id,
                Timestamp = a.CreatedOn,
                EventType = a.EventType.ToString(),
                Category = a.EntityName, // Map EntityName to Category
                EntityName = a.EntityName,
                EntityType = a.EntityName, // Map EntityName to EntityType
                EntityId = a.EntityId?.ToString(),
                Action = a.Action,
                Description = a.Description,
                Details = a.Description, // Map Description to Details
                IpAddress = a.IpAddress,
                UserId = a.UserId,
                UserName = a.User?.FirstName + " " + a.User?.LastName,
                UserEmail = a.User?.Email?.Value,
                UserAgent = a.UserAgent,
                IsSuccess = a.IsSuccess,
                ErrorMessage = a.ErrorMessage,
                Severity = a.RiskLevel, // Map RiskLevel to Severity
                RequiresAttention = a.RequiresAttention,
                IsReviewed = a.IsReviewed,
                ReviewedBy = a.ReviewedBy,
                ReviewedOn = a.ReviewedDate, // Map ReviewedDate to ReviewedOn
                ReviewComments = a.ReviewComments,
                HttpMethod = a.HttpMethod,
                RequestPath = a.RequestPath,
                ResponseStatusCode = a.ResponseStatusCode,
                Duration = a.Duration,
                CreatedOn = a.CreatedOn
            }).ToList();

            // Use the correct PagedResultDto.Create method
            return PagedResultDto<AuditLogDto>.Create(auditLogDtos, totalCount, request.PageIndex, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system events");
            throw;
        }
    }
}
