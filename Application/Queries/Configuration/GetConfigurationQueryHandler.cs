using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Configuration;

/// <summary>
/// Handler for getting a specific configuration
/// </summary>
public class GetConfigurationQueryHandler : IRequestHandler<GetConfigurationQuery, ConfigurationDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetConfigurationQueryHandler> _logger;

    public GetConfigurationQueryHandler(IUnitOfWork unitOfWork, ILogger<GetConfigurationQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ConfigurationDto?> Handle(GetConfigurationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting configuration {Category}.{Key}", request.Category, request.Key);

            var configuration = await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync(request.Category, request.Key, cancellationToken);

            if (configuration == null)
            {
                _logger.LogWarning("Configuration not found: {Category}.{Key}", request.Category, request.Key);
                return null;
            }

            return new ConfigurationDto
            {
                Id = configuration.Id,
                Category = configuration.Category,
                Key = configuration.Key,
                Value = configuration.IsSensitive ? "***" : configuration.Value,
                DataType = configuration.DataType,
                Description = configuration.Description,
                RequiresRestart = configuration.RequiresRestart,
                IsEncrypted = configuration.IsEncrypted,
                IsSensitive = configuration.IsSensitive,
                IsReadOnly = configuration.IsReadOnly,
                DefaultValue = configuration.DefaultValue,
                ValidationRules = configuration.ValidationRules,
                Group = configuration.Group,
                Environment = configuration.Environment,
                DisplayOrder = configuration.DisplayOrder,
                CreatedAt = configuration.CreatedOn,
                CreatedOn = configuration.CreatedOn,
                CreatedBy = configuration.CreatedBy ?? 0,
                ModifiedAt = configuration.ModifiedOn,
                ModifiedOn = configuration.ModifiedOn,
                ModifiedBy = configuration.ModifiedBy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration {Category}.{Key}", request.Category, request.Key);
            throw;
        }
    }
}
