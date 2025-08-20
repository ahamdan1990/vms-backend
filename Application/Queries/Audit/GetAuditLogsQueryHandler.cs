using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Audit;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Audit;

/// <summary>
/// Handler for getting audit logs with filtering and pagination
/// </summary>
public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResultDto<AuditLogDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAuditLogsQueryHandler> _logger;

    public GetAuditLogsQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAuditLogsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResultDto<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting audit logs with filters. Page: {PageIndex}, Size: {PageSize}", 
                request.PageIndex, request.PageSize);

            var auditLogs = await _unitOfWork.AuditLogs.GetAllAsync(cancellationToken);

            // Apply filters based on actual AuditLog properties
            var filteredLogs = auditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                filteredLogs = filteredLogs.Where(a => 
                    a.Action.ToLower().Contains(searchTerm) ||
                    (a.Description != null && a.Description.ToLower().Contains(searchTerm)) ||
                    a.EntityName.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                // Map category to EntityName since Category property doesn't exist
                filteredLogs = filteredLogs.Where(a => a.EntityName.Contains(request.Category));
            }

            if (request.UserId.HasValue)
            {
                filteredLogs = filteredLogs.Where(a => a.UserId == request.UserId.Value);
            }

            if (!string.IsNullOrEmpty(request.Action))
            {
                filteredLogs = filteredLogs.Where(a => a.Action == request.Action);
            }

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
                Category = a.EntityName, // Map EntityName to Category for display
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
            _logger.LogError(ex, "Error retrieving audit logs");
            throw;
        }
    }
}
