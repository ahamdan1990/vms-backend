using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;
using VisitorManagementSystem.Api.Application.Services.Capacity;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Capacity;

/// <summary>
/// Handler for capacity overview query
/// </summary>
public class GetCapacityOverviewQueryHandler : IRequestHandler<GetCapacityOverviewQuery, List<LocationCapacityOverviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICapacityService _capacityService;
    private readonly ILogger<GetCapacityOverviewQueryHandler> _logger;

    public GetCapacityOverviewQueryHandler(
        IUnitOfWork unitOfWork,
        ICapacityService capacityService,
        ILogger<GetCapacityOverviewQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _capacityService = capacityService ?? throw new ArgumentNullException(nameof(capacityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<LocationCapacityOverviewDto>> Handle(
        GetCapacityOverviewQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var checkDateTime = request.DateTime;
            
            _logger.LogDebug("Processing capacity overview query for {DateTime}. Location IDs: {LocationIds}",
                checkDateTime, request.LocationIds != null ? string.Join(",", request.LocationIds) : "all");

            // Get locations based on request parameters
            List<Domain.Entities.Location> locations;

            if (request.LocationIds != null && request.LocationIds.Length > 0)
            {
                // Get specific locations by IDs
                locations = new List<Domain.Entities.Location>();
                foreach (var locationId in request.LocationIds)
                {
                    var location = await _unitOfWork.Locations.GetByIdAsync(locationId, cancellationToken);
                    if (location != null)
                    {
                        locations.Add(location);
                    }
                }
            }
            else
            {
                // Get all active locations
                locations = await _unitOfWork.Locations.GetOrderedAsync(cancellationToken);
            }

            // Filter inactive locations if not requested
            if (!request.IncludeInactive)
            {
                locations = locations.Where(l => l.IsActive).ToList();
            }

            // Build capacity overview for each location
            var capacityOverview = new List<LocationCapacityOverviewDto>();

            foreach (var location in locations)
            {
                try
                {
                    // Get current occupancy for this location
                    var currentOccupancy = await _capacityService.GetCurrentOccupancyAsync(
                        checkDateTime, location.Id, cancellationToken);
                    
                    var maxCapacity = location.MaxCapacity;
                    
                    // Calculate derived values
                    var availableSlots = Math.Max(0, maxCapacity - currentOccupancy);
                    var occupancyPercentage = maxCapacity > 0 
                        ? Math.Round((decimal)currentOccupancy / maxCapacity * 100, 2) 
                        : 0;
                    var isAtCapacity = currentOccupancy >= maxCapacity;
                    var isWarningLevel = maxCapacity > 0 && (currentOccupancy / (decimal)maxCapacity) >= 0.8m && !isAtCapacity;
                    var isOverCapacity = currentOccupancy > maxCapacity;

                    var capacityDto = new LocationCapacityOverviewDto
                    {
                        LocationId = location.Id,
                        LocationName = location.Name,
                        LocationCode = location.Code,
                        Building = location.Building,
                        Floor = location.Floor,
                        MaxCapacity = maxCapacity,
                        CurrentOccupancy = currentOccupancy,
                        ReservedCount = 0, // Could be enhanced to track reservations
                        AvailableSlots = availableSlots,
                        OccupancyPercentage = occupancyPercentage,
                        IsAtCapacity = isAtCapacity,
                        IsWarningLevel = isWarningLevel,
                        IsOverCapacity = isOverCapacity,
                        LastUpdated = DateTime.UtcNow,
                        TimeSlots = new List<TimeSlotCapacityDto>() // Could be enhanced to include time slot breakdown
                    };

                    capacityOverview.Add(capacityDto);

                    _logger.LogDebug("Capacity calculated for location {LocationName} (ID: {LocationId}): " +
                        "Occupancy {CurrentOccupancy}/{MaxCapacity} ({OccupancyPercentage}%)", 
                        location.Name, location.Id, currentOccupancy, maxCapacity, occupancyPercentage);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get capacity data for location {LocationName} (ID: {LocationId}). " +
                        "Adding location with zero occupancy.", location.Name, location.Id);
                    
                    // Add location with default values if capacity calculation fails
                    var fallbackDto = new LocationCapacityOverviewDto
                    {
                        LocationId = location.Id,
                        LocationName = location.Name,
                        LocationCode = location.Code,
                        Building = location.Building,
                        Floor = location.Floor,
                        MaxCapacity = location.MaxCapacity,
                        CurrentOccupancy = 0,
                        ReservedCount = 0,
                        AvailableSlots = location.MaxCapacity,
                        OccupancyPercentage = 0,
                        IsAtCapacity = false,
                        IsWarningLevel = false,
                        IsOverCapacity = false,
                        LastUpdated = DateTime.UtcNow,
                        TimeSlots = new List<TimeSlotCapacityDto>()
                    };

                    capacityOverview.Add(fallbackDto);
                }
            }

            _logger.LogDebug("Capacity overview generated for {LocationCount} locations", capacityOverview.Count);
            return capacityOverview;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing capacity overview query");
            throw;
        }
    }
}
