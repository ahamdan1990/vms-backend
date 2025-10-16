using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Audit;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Audit;

/// <summary>
/// Handler for marking audit logs as reviewed
/// </summary>
public class MarkAuditLogsReviewedCommandHandler : IRequestHandler<MarkAuditLogsReviewedCommand, MarkReviewedResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkAuditLogsReviewedCommandHandler> _logger;

    public MarkAuditLogsReviewedCommandHandler(IUnitOfWork unitOfWork, ILogger<MarkAuditLogsReviewedCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MarkReviewedResultDto> Handle(MarkAuditLogsReviewedCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Marking {Count} audit logs as reviewed by user {UserId}", 
                request.AuditLogIds.Count, request.ReviewedBy);

            // Find the audit logs to mark as reviewed
            var auditLogs = await _unitOfWork.AuditLogs.GetAllAsync(cancellationToken);
            var logsToUpdate = auditLogs.Where(a => request.AuditLogIds.Contains(a.Id)).ToList();

            if (!logsToUpdate.Any())
            {
                _logger.LogWarning("No audit logs found with the provided IDs: {AuditLogIds}", 
                    string.Join(", ", request.AuditLogIds));

                return new MarkReviewedResultDto
                {
                    Success = false,
                    ErrorMessage = "No audit logs found with the provided IDs",
                    UpdatedCount = 0,
                    FailedIds = request.AuditLogIds
                };
            }

            var updatedCount = 0;
            var failedIds = new List<int>();

            // Update each audit log using the entity's built-in method
            foreach (var auditLog in logsToUpdate)
            {
                try
                {
                    auditLog.MarkAsReviewed(request.ReviewedBy, request.ReviewComments);
                    _unitOfWork.AuditLogs.Update(auditLog);
                    updatedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to mark audit log {AuditLogId} as reviewed", auditLog.Id);
                    failedIds.Add(auditLog.Id);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully marked {UpdatedCount} audit logs as reviewed by user {UserId}", 
                updatedCount, request.ReviewedBy);

            return new MarkReviewedResultDto
            {
                Success = updatedCount > 0,
                UpdatedCount = updatedCount,
                ReviewedCount = updatedCount,
                ReviewedBy = request.ReviewedBy,
                ReviewedOn = DateTime.UtcNow,
                FailedIds = failedIds,
                Message = $"Successfully marked {updatedCount} audit logs as reviewed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking audit logs as reviewed");
            
            return new MarkReviewedResultDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                UpdatedCount = 0,
                FailedIds = request.AuditLogIds
            };
        }
    }
}
