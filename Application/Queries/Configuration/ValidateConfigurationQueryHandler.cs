using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Configuration;

/// <summary>
/// Handler for validating configuration values
/// </summary>
public class ValidateConfigurationQueryHandler : IRequestHandler<ValidateConfigurationQuery, ConfigurationValidationResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDynamicConfigurationService _configService;
    private readonly ILogger<ValidateConfigurationQueryHandler> _logger;

    public ValidateConfigurationQueryHandler(
        IUnitOfWork unitOfWork,
        IDynamicConfigurationService configService,
        ILogger<ValidateConfigurationQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _configService = configService;
        _logger = logger;
    }

    public async Task<ConfigurationValidationResultDto> Handle(ValidateConfigurationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Validating configuration {Category}.{Key} with value", request.Category, request.Key);

            var configuration = await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync(request.Category, request.Key, cancellationToken);

            if (configuration == null)
            {
                return new ConfigurationValidationResultDto
                {
                    IsValid = false,
                    ErrorMessage = $"Configuration '{request.Category}.{request.Key}' not found",
                    Errors = new List<string> { "Configuration not found" }
                };
            }

            // Use the configuration service validation
            var validationResult = await _configService.ValidateConfigurationAsync(request.Category, request.Key, request.Value, cancellationToken);

            if (validationResult.IsValid)
            {
                return new ConfigurationValidationResultDto
                {
                    IsValid = true,
                    Message = "Configuration value is valid",
                    Errors = new List<string>(),
                    Warnings = validationResult.Warnings
                };
            }

            return new ConfigurationValidationResultDto
            {
                IsValid = false,
                ErrorMessage = "Configuration validation failed",
                Message = "Validation failed with errors",
                Errors = validationResult.Errors,
                Warnings = validationResult.Warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration {Category}.{Key}", request.Category, request.Key);
            
            return new ConfigurationValidationResultDto
            {
                IsValid = false,
                ErrorMessage = $"Validation error: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
