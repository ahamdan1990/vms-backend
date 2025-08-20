using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Configuration;

/// <summary>
/// Handler for getting configuration history
/// </summary>
public class GetConfigurationHistoryQueryHandler : IRequestHandler<GetConfigurationHistoryQuery, List<ConfigurationHistoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetConfigurationHistoryQueryHandler> _logger;

    public GetConfigurationHistoryQueryHandler(IUnitOfWork unitOfWork, ILogger<GetConfigurationHistoryQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<ConfigurationHistoryDto>> Handle(GetConfigurationHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting configuration history for {Category}.{Key}", request.Category, request.Key);

            // First get the configuration to get its ID
            var configuration = await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync(request.Category, request.Key, cancellationToken);
            
            if (configuration == null)
            {
                _logger.LogWarning("Configuration not found: {Category}.{Key}", request.Category, request.Key);
                return new List<ConfigurationHistoryDto>();
            }

            // Get audit history for this configuration
            var auditEntries = await _unitOfWork.ConfigurationAudits.GetAsync(
                a => a.SystemConfigurationId == configuration.Id, 
                cancellationToken);

            var orderedEntries = auditEntries
                .OrderByDescending(a => a.CreatedOn)
                .Take(request.PageSize);

            return orderedEntries.Select(a => new ConfigurationHistoryDto
            {
                Id = a.Id,
                ConfigurationId = a.SystemConfigurationId,
                Category = a.Category,
                Key = a.Key,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                Action = a.Action,
                Reason = a.Reason,
                UserId = a.CreatedBy ?? 0,
                ChangedBy = a.CreatedBy ?? 0,
                UserName = "Unknown", // Would need to join with Users table
                ChangedByName = "Unknown", // Would need to join with Users table
                Timestamp = a.CreatedOn,
                ChangedOn = a.CreatedOn
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration history for {Category}.{Key}", request.Category, request.Key);
            throw;
        }
    }
}
