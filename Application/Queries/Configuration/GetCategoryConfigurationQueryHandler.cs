using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Configuration;

/// <summary>
/// Handler for getting configurations by category
/// </summary>
public class GetCategoryConfigurationQueryHandler : IRequestHandler<GetCategoryConfigurationQuery, List<ConfigurationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetCategoryConfigurationQueryHandler> _logger;

    public GetCategoryConfigurationQueryHandler(IUnitOfWork unitOfWork, ILogger<GetCategoryConfigurationQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<ConfigurationDto>> Handle(GetCategoryConfigurationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting configurations for category: {Category}", request.Category);

            var configurations = await _unitOfWork.SystemConfigurations.GetByCategoryAsync(request.Category, cancellationToken);

            var orderedConfigs = configurations
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Key);

            return orderedConfigs.Select(c => new ConfigurationDto
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
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations for category: {Category}", request.Category);
            throw;
        }
    }
}
