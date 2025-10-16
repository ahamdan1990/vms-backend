using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Audit;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Audit;

/// <summary>
/// Handler for searching audit logs
/// </summary>
public class SearchAuditLogsQueryHandler : IRequestHandler<SearchAuditLogsQuery, PagedResultDto<AuditLogDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SearchAuditLogsQueryHandler> _logger;

    public SearchAuditLogsQueryHandler(IUnitOfWork unitOfWork, ILogger<SearchAuditLogsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResultDto<AuditLogDto>> Handle(SearchAuditLogsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Searching audit logs with term: {SearchTerm}", request.SearchTerm);

            var auditLogs = await _unitOfWork.AuditLogs.GetAllAsync(cancellationToken);

            // Apply search filters based on actual AuditLog properties
            var filteredLogs = auditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                filteredLogs = filteredLogs.Where(a =>
                    a.Action.ToLower().Contains(searchTerm) ||
                    (a.Description != null && a.Description.ToLower().Contains(searchTerm)) ||
                    a.EntityName.ToLower().Contains(searchTerm) ||
                    (a.UserAgent != null && a.UserAgent.ToLower().Contains(searchTerm)) ||
                    (a.IpAddress != null && a.IpAddress.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(request.Category))
            {
                // Map category to EntityName since Category property doesn't exist
                filteredLogs = filteredLogs.Where(a => a.EntityName.Contains(request.Category));
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
            _logger.LogError(ex, "Error searching audit logs with term: {SearchTerm}", request.SearchTerm);
            throw;
        }
    }
}
