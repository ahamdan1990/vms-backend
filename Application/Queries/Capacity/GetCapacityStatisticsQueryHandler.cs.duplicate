using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;
using VisitorManagementSystem.Api.Application.Services.Capacity;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Capacity;

/// <summary>
/// Handler for capacity statistics query
/// </summary>
public class GetCapacityStatisticsQueryHandler : IRequestHandler<GetCapacityStatisticsQuery, CapacityStatisticsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICapacityService _capacityService;
    private readonly ILogger<GetCapacityStatisticsQueryHandler> _logger;

    public GetCapacityStatisticsQueryHandler(
        IUnitOfWork unitOfWork,
        ICapacityService capacityService,
        ILogger<GetCapacityStatisticsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _capacityService = capacityService ?? throw new ArgumentNullException(nameof(capacityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CapacityStatisticsDto> Handle(
        GetCapacityStatisticsQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing capacity statistics query for period {StartDate} to {EndDate}, Location: {LocationId}",
                request.StartDate, request.EndDate, request.LocationId);

            // Get location name if specific location requested
            string? locationName = null;
            if (request.LocationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(request.LocationId.Value, cancellationToken);
                locationName = location?.Name;
                
                if (location == null)
                {
                    throw new ArgumentException($"Location with ID {request.LocationId} not found");
                }
            }

            // Call the capacity service to get statistics
            var statistics = await _capacityService.GetOccupancyStatisticsAsync(
                request.StartDate, request.EndDate, request.LocationId, cancellationToken);

            // Since the service returns an object, we need to convert it to our expected DTO format
            // For now, we'll create a basic structure - this would need to be enhanced based on actual service implementation
            var statisticsDto = new CapacityStatisticsDto
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                LocationId = request.LocationId,
                LocationName = locationName,
                TotalCapacity = 0,     // Would be calculated from actual statistics
                TotalBookings = 0,     // Would be calculated from actual statistics
                PeakOccupancy = 0,     // Would be calculated from actual statistics
                AverageOccupancy = 0,  // Would be calculated from actual statistics
                UtilizationPercentage = 0, // Would be calculated from actual statistics
                DaysAtCapacity = 0,    // Would be calculated from actual statistics
                DaysAtWarning = 0,     // Would be calculated from actual statistics
                PeakOccupancyDate = null, // Would be calculated from actual statistics
                DailyBreakdown = new List<DailyCapacityDto>() // Would be populated from actual statistics
            };

            _logger.LogDebug("Capacity statistics generated for {DaysCount} day period", 
                (request.EndDate - request.StartDate).Days + 1);

            return statisticsDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing capacity statistics query");
            throw;
        }
    }
}
