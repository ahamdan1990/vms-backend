using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;
using VisitorManagementSystem.Api.Application.Services.Capacity;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Capacity;

/// <summary>
/// Handler for occupancy query
/// </summary>
public class GetOccupancyQueryHandler : IRequestHandler<GetOccupancyQuery, OccupancyDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICapacityService _capacityService;
    private readonly ILogger<GetOccupancyQueryHandler> _logger;

    public GetOccupancyQueryHandler(
        IUnitOfWork unitOfWork,
        ICapacityService capacityService,
        ILogger<GetOccupancyQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _capacityService = capacityService ?? throw new ArgumentNullException(nameof(capacityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OccupancyDto> Handle(GetOccupancyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing occupancy query for {DateTime}, Location: {LocationId}",
                request.DateTime, request.LocationId);

            // Get current occupancy and max capacity
            int currentOccupancy;
            int maxCapacity;
            if (request.LocationId.HasValue)
            {
                // Location-specific
                currentOccupancy = await _capacityService.GetCurrentOccupancyAsync(
                    request.DateTime, request.LocationId, cancellationToken);

                maxCapacity = await _capacityService.GetMaxCapacityAsync(
                    request.DateTime, request.LocationId, cancellationToken);
            }
            else
            {
                // System-wide
                currentOccupancy = await _capacityService.GetSystemWideOccupancyAsync(
                    request.DateTime, cancellationToken);

                maxCapacity = await _capacityService.GetSystemWideMaxCapacityAsync(
                    request.DateTime, cancellationToken);
            }

            // Get location information if specific location requested
            string? locationName = null;
            if (request.LocationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(request.LocationId.Value, cancellationToken);
                locationName = location?.Name;
            }

            // Calculate derived values
            var availableSlots = Math.Max(0, maxCapacity - currentOccupancy);
            var occupancyPercentage = maxCapacity > 0 
                ? Math.Round((decimal)currentOccupancy / maxCapacity * 100, 2) 
                : 0;
            var isAtCapacity = currentOccupancy >= maxCapacity;
            var isWarningLevel = maxCapacity > 0 && (currentOccupancy / (decimal)maxCapacity) >= 0.8m && !isAtCapacity;

            var occupancyDto = new OccupancyDto
            {
                CurrentOccupancy = currentOccupancy,
                MaxCapacity = maxCapacity,
                AvailableSlots = availableSlots,
                OccupancyPercentage = occupancyPercentage,
                IsAtCapacity = isAtCapacity,
                IsWarningLevel = isWarningLevel,
                DateTime = request.DateTime,
                LocationId = request.LocationId,
                LocationName = locationName,
                TimeSlotId = null, // Could be enhanced to support time slots
                TimeSlotName = null,
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogDebug("Occupancy calculated: {CurrentOccupancy}/{MaxCapacity} ({OccupancyPercentage}%)",
                currentOccupancy, maxCapacity, occupancyPercentage);

            return occupancyDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing occupancy query");
            throw;
        }
    }
}
