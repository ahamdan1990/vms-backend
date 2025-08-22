using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;
using VisitorManagementSystem.Api.Application.Services.Capacity;

namespace VisitorManagementSystem.Api.Application.Queries.Capacity;

/// <summary>
/// Handler for alternative time slots query
/// </summary>
public class GetAlternativeTimeSlotsQueryHandler : IRequestHandler<GetAlternativeTimeSlotsQuery, List<AlternativeTimeSlotDto>>
{
    private readonly ICapacityService _capacityService;
    private readonly ILogger<GetAlternativeTimeSlotsQueryHandler> _logger;

    public GetAlternativeTimeSlotsQueryHandler(
        ICapacityService capacityService,
        ILogger<GetAlternativeTimeSlotsQueryHandler> logger)
    {
        _capacityService = capacityService ?? throw new ArgumentNullException(nameof(capacityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<AlternativeTimeSlotDto>> Handle(
        GetAlternativeTimeSlotsQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing alternative time slots query for {OriginalDateTime}, Expected visitors: {ExpectedVisitors}, Location: {LocationId}",
                request.OriginalDateTime, request.ExpectedVisitors, request.LocationId);

            // Call the capacity service to get alternative time slots
            var alternatives = await _capacityService.GetAlternativeTimeSlotsAsync(
                request.OriginalDateTime, 
                request.ExpectedVisitors, 
                request.LocationId, 
                cancellationToken);

            _logger.LogDebug("Found {AlternativesCount} alternative time slots", alternatives.Count);

            return alternatives;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing alternative time slots query");
            throw;
        }
    }
}
