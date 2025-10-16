using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Configuration;

/// <summary>
/// Handler for searching configurations
/// </summary>
public class SearchConfigurationsQueryHandler : IRequestHandler<SearchConfigurationsQuery, List<ConfigurationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SearchConfigurationsQueryHandler> _logger;

    public SearchConfigurationsQueryHandler(IUnitOfWork unitOfWork, ILogger<SearchConfigurationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<ConfigurationDto>> Handle(SearchConfigurationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Searching configurations with term: {SearchTerm}", request.SearchTerm);

            var configurations = await _unitOfWork.SystemConfigurations.SearchAsync(request.SearchTerm, cancellationToken);

            var orderedConfigs = configurations
                .OrderBy(c => c.Category)
                .ThenBy(c => c.DisplayOrder)
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
            _logger.LogError(ex, "Error searching configurations with term: {SearchTerm}", request.SearchTerm);
            throw;
        }
    }
}
