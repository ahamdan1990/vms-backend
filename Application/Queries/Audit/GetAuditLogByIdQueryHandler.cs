using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Audit;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Audit;

/// <summary>
/// Handler for getting a specific audit log by ID
/// </summary>
public class GetAuditLogByIdQueryHandler : IRequestHandler<GetAuditLogByIdQuery, AuditLogDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAuditLogByIdQueryHandler> _logger;

    public GetAuditLogByIdQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAuditLogByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuditLogDetailDto?> Handle(GetAuditLogByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting audit log by ID: {AuditLogId}", request.Id);

            var auditLog = await _unitOfWork.AuditLogs.GetByIdAsync(request.Id, cancellationToken);

            if (auditLog == null)
            {
                _logger.LogWarning("Audit log not found with ID: {AuditLogId}", request.Id);
                return null;
            }

            return new AuditLogDetailDto
            {
                Id = auditLog.Id,
                Timestamp = auditLog.CreatedOn,
                EventType = auditLog.EventType.ToString(),
                Category = auditLog.EntityName, // Map EntityName to Category
                EntityName = auditLog.EntityName,
                EntityType = auditLog.EntityName, // Map EntityName to EntityType
                EntityId = auditLog.EntityId?.ToString(),
                Action = auditLog.Action,
                Description = auditLog.Description,
                Details = auditLog.Description, // Map Description to Details
                IpAddress = auditLog.IpAddress,
                UserId = auditLog.UserId,
                UserName = auditLog.User?.FirstName + " " + auditLog.User?.LastName,
                UserEmail = auditLog.User?.Email?.Value,
                UserAgent = auditLog.UserAgent,
                IsSuccess = auditLog.IsSuccess,
                ErrorMessage = auditLog.ErrorMessage,
                Severity = auditLog.RiskLevel, // Map RiskLevel to Severity
                RequiresAttention = auditLog.RequiresAttention,
                IsReviewed = auditLog.IsReviewed,
                ReviewedBy = auditLog.ReviewedBy,
                ReviewedOn = auditLog.ReviewedDate, // Map ReviewedDate to ReviewedOn
                ReviewComments = auditLog.ReviewComments,
                HttpMethod = auditLog.HttpMethod,
                RequestPath = auditLog.RequestPath,
                ResponseStatusCode = auditLog.ResponseStatusCode,
                Duration = auditLog.Duration,
                CreatedOn = auditLog.CreatedOn,
                // Additional detailed fields
                OldValues = auditLog.OldValues,
                NewValues = auditLog.NewValues,
                Metadata = auditLog.Metadata,
                CorrelationId = auditLog.CorrelationId,
                SessionId = auditLog.SessionId,
                RequestId = auditLog.RequestId,
                RequestSize = auditLog.RequestSize,
                ResponseSize = auditLog.ResponseSize,
                ExceptionDetails = auditLog.ExceptionDetails,
                ReviewedByName = auditLog.ReviewedBy.HasValue ? "Unknown" : null, // Would need user lookup
                ReviewedDate = auditLog.ReviewedDate,
                ModifiedOn = auditLog.ModifiedOn,
                ModifiedBy = auditLog.UserId // AuditLog doesn't have ModifiedBy, using UserId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit log by ID: {AuditLogId}", request.Id);
            throw;
        }
    }
}
