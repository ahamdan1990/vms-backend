using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Configuration;

/// <summary>
/// Handler for deleting a configuration
/// </summary>
public class DeleteConfigurationCommandHandler : IRequestHandler<DeleteConfigurationCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDynamicConfigurationService _configService;
    private readonly ILogger<DeleteConfigurationCommandHandler> _logger;

    public DeleteConfigurationCommandHandler(
        IUnitOfWork unitOfWork,
        IDynamicConfigurationService configService,
        ILogger<DeleteConfigurationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _configService = configService;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteConfigurationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting configuration {Category}.{Key}", request.Category, request.Key);

            var configuration = await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync(request.Category, request.Key, cancellationToken);

            if (configuration == null)
            {
                _logger.LogWarning("Configuration not found for deletion: {Category}.{Key}", request.Category, request.Key);
                return false;
            }

            if (configuration.IsReadOnly)
            {
                _logger.LogWarning("Attempt to delete read-only configuration: {Category}.{Key}", request.Category, request.Key);
                throw new InvalidOperationException($"Configuration '{request.Category}.{request.Key}' is read-only and cannot be deleted");
            }

            // Create audit record before deletion
            var auditRecord = new ConfigurationAudit
            {
                SystemConfigurationId = configuration.Id,
                Category = configuration.Category,
                Key = configuration.Key,
                OldValue = configuration.Value,
                NewValue = string.Empty,
                Action = "Delete",
                Reason = request.Reason ?? "Configuration deleted",
                CreatedBy = request.DeletedBy
            };

            await _unitOfWork.ConfigurationAudits.AddAsync(auditRecord, cancellationToken);

            // Delete the configuration
            _unitOfWork.SystemConfigurations.Remove(configuration);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate cache for this category
            await _configService.InvalidateCacheAsync(request.Category);

            _logger.LogInformation("Configuration deleted successfully: {Category}.{Key} by user {UserId}", 
                request.Category, request.Key, request.DeletedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {Category}.{Key}", request.Category, request.Key);
            throw;
        }
    }
}
