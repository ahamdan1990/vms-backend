using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Configuration;

/// <summary>
/// Handler for creating a new configuration
/// </summary>
public class CreateConfigurationCommandHandler : IRequestHandler<CreateConfigurationCommand, ConfigurationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDynamicConfigurationService _configService;
    private readonly ILogger<CreateConfigurationCommandHandler> _logger;

    public CreateConfigurationCommandHandler(
        IUnitOfWork unitOfWork,
        IDynamicConfigurationService configService,
        ILogger<CreateConfigurationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _configService = configService;
        _logger = logger;
    }

    public async Task<ConfigurationDto> Handle(CreateConfigurationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating configuration {Category}.{Key}", request.Category, request.Key);

            // Check if configuration already exists
            var existingConfig = await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync(request.Category, request.Key, cancellationToken);

            if (existingConfig != null)
            {
                _logger.LogWarning("Attempt to create duplicate configuration: {Category}.{Key}", request.Category, request.Key);
                throw new InvalidOperationException($"Configuration with category '{request.Category}' and key '{request.Key}' already exists");
            }

            // Create new configuration
            var configuration = new SystemConfiguration
            {
                Category = request.Category,
                Key = request.Key,
                Value = request.Value,
                DataType = request.DataType,
                Description = request.Description,
                RequiresRestart = request.RequiresRestart,
                IsEncrypted = request.IsEncrypted,
                IsSensitive = request.IsSensitive,
                IsReadOnly = request.IsReadOnly,
                DefaultValue = request.DefaultValue,
                ValidationRules = request.ValidationRules,
                Group = request.Group,
                Environment = request.Environment ?? "All",
                DisplayOrder = request.DisplayOrder
            };

            configuration.SetCreatedBy(request.CreatedBy);

            await _unitOfWork.SystemConfigurations.AddAsync(configuration, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Configuration created successfully: {Category}.{Key} with ID {Id}", 
                request.Category, request.Key, configuration.Id);

            // Return DTO
            return new ConfigurationDto
            {
                Id = configuration.Id,
                Category = configuration.Category,
                Key = configuration.Key,
                Value = configuration.IsSensitive ? "***" : configuration.Value,
                DataType = configuration.DataType,
                Description = configuration.Description,
                IsReadOnly = configuration.IsReadOnly,
                IsEncrypted = configuration.IsEncrypted,
                IsSensitive = configuration.IsSensitive,
                RequiresRestart = configuration.RequiresRestart,
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
            _logger.LogError(ex, "Error creating configuration {Category}.{Key}", request.Category, request.Key);
            throw;
        }
    }
}
