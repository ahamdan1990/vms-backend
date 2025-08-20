using MediatR;
using System.Text;
using VisitorManagementSystem.Api.Application.DTOs.Audit;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Audit;

/// <summary>
/// Handler for exporting audit logs to various formats
/// </summary>
public class ExportAuditLogsCommandHandler : IRequestHandler<ExportAuditLogsCommand, ExportAuditLogsResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExportAuditLogsCommandHandler> _logger;

    public ExportAuditLogsCommandHandler(IUnitOfWork unitOfWork, ILogger<ExportAuditLogsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ExportAuditLogsResultDto> Handle(ExportAuditLogsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting audit logs export. Format: {Format}, MaxRecords: {MaxRecords}", 
                request.Format, request.MaxRecords);

            // Get audit logs with filters
            var query = await _unitOfWork.AuditLogs.GetAllAsync(cancellationToken);

            // Apply filters based on actual AuditLog properties
            var filteredLogs = query.AsQueryable();

            if (!string.IsNullOrEmpty(request.Category))
            {
                // Map category filter to EntityName since Category property doesn't exist
                filteredLogs = filteredLogs.Where(a => a.EntityName.Contains(request.Category) || a.Action.Contains(request.Category));
            }

            if (request.DateFrom.HasValue)
            {
                filteredLogs = filteredLogs.Where(a => a.CreatedOn >= request.DateFrom.Value);
            }

            if (request.DateTo.HasValue)
            {
                filteredLogs = filteredLogs.Where(a => a.CreatedOn <= request.DateTo.Value);
            }

            // Apply ordering and limit
            var auditLogs = filteredLogs
                .OrderByDescending(a => a.CreatedOn)
                .Take(request.MaxRecords)
                .ToList();

            byte[] fileContent;
            string fileName;
            string contentType;

            // Generate export based on format
            if (request.Format.ToLower() == "csv")
            {
                fileContent = GenerateCsvExport(auditLogs);
                fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                contentType = "text/csv";
            }
            else
            {
                fileContent = GenerateExcelExport(auditLogs);
                fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }

            _logger.LogInformation("Audit logs export completed. Records: {RecordCount}, Format: {Format}", 
                auditLogs.Count, request.Format);

            return new ExportAuditLogsResultDto
            {
                Success = true,
                FileContent = fileContent,
                Data = fileContent,
                FileName = fileName,
                ContentType = contentType,
                Format = request.Format,
                RecordCount = auditLogs.Count,
                ExportedAt = DateTime.UtcNow,
                ExportedBy = request.ExportedBy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            
            return new ExportAuditLogsResultDto
            {
                Success = false,
                ErrorMessage = ex.Message,
                RecordCount = 0
            };
        }
    }

    private byte[] GenerateCsvExport(List<AuditLog> auditLogs)
    {
        var csv = new StringBuilder();
        
        // CSV Header
        csv.AppendLine("Id,Timestamp,EventType,EntityName,EntityId,Action,Description,UserId,UserName,IpAddress,IsSuccess,ErrorMessage,RiskLevel,RequiresAttention,IsReviewed");

        // CSV Data
        foreach (var log in auditLogs)
        {
            csv.AppendLine($"{log.Id}," +
                          $"{log.CreatedOn:yyyy-MM-dd HH:mm:ss}," +
                          $"{log.EventType}," +
                          $"\"{log.EntityName}\"," +
                          $"{log.EntityId}," +
                          $"\"{log.Action}\"," +
                          $"\"{log.Description?.Replace("\"", "\"\"")}\"," +
                          $"{log.UserId}," +
                          $"\"{log.User?.FirstName} {log.User?.LastName}\"," +
                          $"{log.IpAddress}," +
                          $"{log.IsSuccess}," +
                          $"\"{log.ErrorMessage?.Replace("\"", "\"\"")}\"," +
                          $"{log.RiskLevel}," +
                          $"{log.RequiresAttention}," +
                          $"{log.IsReviewed}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private byte[] GenerateExcelExport(List<AuditLog> auditLogs)
    {
        // For simplicity, return CSV format for now
        // In a real implementation, you would use a library like EPPlus or ClosedXML
        return GenerateCsvExport(auditLogs);
    }
}
