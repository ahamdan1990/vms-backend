using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;
using VisitorManagementSystem.Api.Application.Services.Capacity;

namespace VisitorManagementSystem.Api.Application.Queries.Capacity;

/// <summary>
/// Handler for capacity validation query
/// </summary>
public class ValidateCapacityQueryHandler : IRequestHandler<ValidateCapacityQuery, CapacityValidationResponseDto>
{
    private readonly ICapacityService _capacityService;
    private readonly ILogger<ValidateCapacityQueryHandler> _logger;

    public ValidateCapacityQueryHandler(
        ICapacityService capacityService,
        ILogger<ValidateCapacityQueryHandler> logger)
    {
        _capacityService = capacityService ?? throw new ArgumentNullException(nameof(capacityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CapacityValidationResponseDto> Handle(
        ValidateCapacityQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing capacity validation query for {ExpectedVisitors} visitors at {DateTime}",
                request.ExpectedVisitors, request.DateTime);

            var validationRequest = new CapacityValidationRequestDto
            {
                LocationId = request.LocationId,
                TimeSlotId = request.TimeSlotId,
                DateTime = request.DateTime,
                ExpectedVisitors = request.ExpectedVisitors,
                IsVipRequest = request.IsVipRequest,
                ExcludeInvitationId = request.ExcludeInvitationId
            };

            var result = await _capacityService.ValidateCapacityAsync(validationRequest, cancellationToken);

            _logger.LogDebug("Capacity validation completed: Available={IsAvailable}, Occupancy={OccupancyPercentage}%",
                result.IsAvailable, result.OccupancyPercentage);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing capacity validation query");
            throw;
        }
    }
}
