using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Configuration;

/// <summary>
/// Handler for updating a configuration
/// </summary>
public class UpdateConfigurationCommandHandler : IRequestHandler<UpdateConfigurationCommand, ConfigurationUpdateResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDynamicConfigurationService _configService;
    private readonly ILogger<UpdateConfigurationCommandHandler> _logger;

    public UpdateConfigurationCommandHandler(
        IUnitOfWork unitOfWork,
        IDynamicConfigurationService configService,
        ILogger<UpdateConfigurationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _configService = configService;
        _logger = logger;
    }

    public async Task<ConfigurationUpdateResultDto> Handle(UpdateConfigurationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating configuration {Category}.{Key}", request.Category, request.Key);

            var configuration = await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync(request.Category, request.Key, cancellationToken);

            if (configuration == null)
            {
                _logger.LogWarning("Configuration not found: {Category}.{Key}", request.Category, request.Key);
                throw new InvalidOperationException($"Configuration with category '{request.Category}' and key '{request.Key}' not found");
            }

            if (configuration.IsReadOnly)
            {
                _logger.LogWarning("Attempt to modify read-only configuration: {Category}.{Key}", request.Category, request.Key);
                throw new InvalidOperationException($"Configuration '{request.Category}.{request.Key}' is read-only and cannot be modified");
            }

            // Store old value for history
            var oldValue = configuration.Value;

            // Create history record
            var configHistory = new ConfigurationAudit
            {
                SystemConfigurationId = configuration.Id,
                Category = configuration.Category,
                Key = configuration.Key,
                OldValue = oldValue,
                NewValue = request.Value,
                Action = "Update",
                Reason = request.Reason ?? "Configuration updated",
                CreatedBy = request.ModifiedBy
            };

            await _unitOfWork.ConfigurationAudits.AddAsync(configHistory, cancellationToken);

            // Update configuration
            configuration.UpdateValue(request.Value, request.ModifiedBy);
            _unitOfWork.SystemConfigurations.Update(configuration);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate cache for this category
            await _configService.InvalidateCacheAsync(request.Category);

            _logger.LogInformation("Configuration updated successfully: {Category}.{Key} by user {UserId}", 
                request.Category, request.Key, request.ModifiedBy);

            return new ConfigurationUpdateResultDto
            {
                Success = true,
                RequiresRestart = configuration.RequiresRestart,
                RestartWarning = configuration.RequiresRestart ? "Application restart required for this change to take effect." : null,
                Message = "Configuration updated successfully",
                UpdatedOn = DateTime.UtcNow,
                UpdatedBy = request.ModifiedBy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration {Category}.{Key}", request.Category, request.Key);
            throw;
        }
    }
}
