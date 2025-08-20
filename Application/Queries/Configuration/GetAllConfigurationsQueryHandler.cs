using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Configuration;

/// <summary>
/// Handler for getting all configurations grouped by category
/// </summary>
public class GetAllConfigurationsQueryHandler : IRequestHandler<GetAllConfigurationsQuery, Dictionary<string, List<ConfigurationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAllConfigurationsQueryHandler> _logger;

    public GetAllConfigurationsQueryHandler(IUnitOfWork unitOfWork, ILogger<GetAllConfigurationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Dictionary<string, List<ConfigurationDto>>> Handle(GetAllConfigurationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting all configurations grouped by category");

            var configurations = await _unitOfWork.SystemConfigurations.GetAllAsync(cancellationToken);

            var orderedConfigs = configurations
                .OrderBy(c => c.Category)
                .ThenBy(c => c.DisplayOrder)
                .ThenBy(c => c.Key);

            var groupedConfigs = orderedConfigs
                .GroupBy(c => c.Category)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(c => new ConfigurationDto
                    {
                        Id = c.Id,
                        Category = c.Category,
                        Key = c.Key,
                        Value = c.IsSensitive ? "***" : c.Value,
                        DataType = c.DataType,
                        Description = c.Description,
                        RequiresRestart = c.RequiresRestart,
                        IsEncrypted = c.IsEncrypted,
                        IsSensitive = c.IsSensitive,
                        IsReadOnly = c.IsReadOnly,
                        DefaultValue = c.DefaultValue,
                        ValidationRules = c.ValidationRules,
                        Group = c.Group,
                        Environment = c.Environment,
                        DisplayOrder = c.DisplayOrder,
                        CreatedAt = c.CreatedOn,
                        CreatedOn = c.CreatedOn,
                        CreatedBy = c.CreatedBy ?? 0,
                        ModifiedAt = c.ModifiedOn,
                        ModifiedOn = c.ModifiedOn,
                        ModifiedBy = c.ModifiedBy
                    }).ToList()
                );

            _logger.LogInformation("Retrieved {ConfigCount} configurations across {CategoryCount} categories", 
                configurations.Count, groupedConfigs.Keys.Count);

            return groupedConfigs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all configurations");
            throw;
        }
    }
}
