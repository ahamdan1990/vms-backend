using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Configuration;

/// <summary>
/// Handler for invalidating configuration cache
/// </summary>
public class InvalidateConfigurationCacheCommandHandler : IRequestHandler<InvalidateConfigurationCacheCommand, CacheInvalidationResultDto>
{
    private readonly IDynamicConfigurationService _configService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InvalidateConfigurationCacheCommandHandler> _logger;

    public InvalidateConfigurationCacheCommandHandler(
        IDynamicConfigurationService configService,
        IUnitOfWork unitOfWork,
        ILogger<InvalidateConfigurationCacheCommandHandler> logger)
    {
        _configService = configService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CacheInvalidationResultDto> Handle(InvalidateConfigurationCacheCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Invalidating configuration cache. Category: {Category}", request.Category ?? "All");

            // Invalidate cache for specific category or all categories
            await _configService.InvalidateCacheAsync(request.Category);

            var scope = string.IsNullOrEmpty(request.Category) ? "All categories" : $"Category: {request.Category}";

            _logger.LogInformation("Configuration cache invalidated successfully. Scope: {Scope} by user {UserId}", 
                scope, request.InvalidatedBy);

            return new CacheInvalidationResultDto
            {
                Success = true,
                Message = $"Configuration cache invalidated successfully for {scope}",
                Category = request.Category,
                InvalidatedAt = DateTime.UtcNow,
                InvalidatedBy = request.InvalidatedBy,
                Scope = scope
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating configuration cache for category: {Category}", request.Category);
            
            return new CacheInvalidationResultDto
            {
                Success = false,
                Message = "Failed to invalidate configuration cache",
                Category = request.Category,
                ErrorMessage = ex.Message,
                InvalidatedAt = DateTime.UtcNow,
                InvalidatedBy = request.InvalidatedBy,
                Scope = string.IsNullOrEmpty(request.Category) ? "All categories" : $"Category: {request.Category}"
            };
        }
    }
}
