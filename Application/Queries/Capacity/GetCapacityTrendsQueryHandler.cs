using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;
using VisitorManagementSystem.Api.Application.Services.Capacity;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Capacity;

/// <summary>
/// Handler for capacity trends query
/// </summary>
public class GetCapacityTrendsQueryHandler : IRequestHandler<GetCapacityTrendsQuery, CapacityTrendsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICapacityService _capacityService;
    private readonly ILogger<GetCapacityTrendsQueryHandler> _logger;

    public GetCapacityTrendsQueryHandler(
        IUnitOfWork unitOfWork,
        ICapacityService capacityService,
        ILogger<GetCapacityTrendsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _capacityService = capacityService ?? throw new ArgumentNullException(nameof(capacityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CapacityTrendsDto> Handle(
        GetCapacityTrendsQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing capacity trends query for period {StartDate} to {EndDate}, Location: {LocationId}, GroupBy: {GroupBy}",
                request.StartDate, request.EndDate, request.LocationId, request.GroupBy);

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

            // Generate time periods based on groupBy parameter
            var periods = GenerateTimePeriods(request.StartDate, request.EndDate, request.GroupBy);
            var dataPoints = new List<CapacityTrendDataPointDto>();

            // Basic implementation - for more sophisticated analytics, 
            // this would need to query historical occupancy data from invitations/visits
            foreach (var period in periods)
            {
                try
                {
                    // For now, we'll provide a basic structure
                    // In a full implementation, this would query actual historical data from invitations table
                    var dataPoint = new CapacityTrendDataPointDto
                    {
                        Period = period.Start,
                        PeriodLabel = FormatPeriodLabel(period.Start, request.GroupBy),
                        MaxCapacity = 0,      // Would be calculated from location(s) capacity
                        AverageOccupancy = 0, // Would be calculated from historical invitation data
                        PeakOccupancy = 0,    // Would be calculated from historical data
                        AverageUtilization = 0, // Would be calculated
                        PeakUtilization = 0,    // Would be calculated
                        TotalVisitors = 0,      // Would be calculated from actual visits
                        WasOverCapacity = false, // Would be determined from data
                        Capacity = 0,           // Additional capacity info
                        Bookings = 0,           // Booking count
                        UtilizationPercentage = 0 // Utilization percentage
                    };

                    dataPoints.Add(dataPoint);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to calculate trends for period {PeriodStart}", period.Start);
                }
            }

            // Calculate summary statistics
            var summary = new CapacityTrendSummaryDto
            {
                OverallAverageUtilization = dataPoints.Count > 0 ? (decimal)dataPoints.Average(dp => dp.AverageUtilization) : 0,
                PeakUtilization = dataPoints.Count > 0 ? (decimal)dataPoints.Max(dp => dp.PeakUtilization) : 0,
                PeakUtilizationDate = dataPoints.OrderByDescending(dp => dp.PeakUtilization).FirstOrDefault()?.Period ?? DateTime.MinValue,
                LowestUtilization = dataPoints.Count > 0 ? (decimal)dataPoints.Min(dp => dp.AverageUtilization) : 0,
                LowestUtilizationDate = dataPoints.OrderBy(dp => dp.AverageUtilization).FirstOrDefault()?.Period ?? DateTime.MinValue,
                TrendDirection = "Stable", // Would be calculated based on trend analysis
                TrendPercentage = 0,       // Would be calculated
                DaysOverCapacity = dataPoints.Count(dp => dp.WasOverCapacity),
                DaysAtWarningLevel = dataPoints.Count(dp => dp.UtilizationPercentage >= 80 && dp.UtilizationPercentage < 100),
                BusiestDayOfWeek = "Monday",    // Would be calculated from data
                QuietestDayOfWeek = "Sunday",   // Would be calculated from data
                BusiestTimeOfDay = new TimeOnly(14, 0),   // Would be calculated from data
                QuietestTimeOfDay = new TimeOnly(9, 0),   // Would be calculated from data
                AverageUtilization = dataPoints.Count > 0 ? dataPoints.Average(dp => dp.UtilizationPercentage) : 0
            };

            var trendsDto = new CapacityTrendsDto
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                LocationId = request.LocationId,
                LocationName = locationName,
                GroupBy = request.GroupBy,
                DataPoints = dataPoints,
                Summary = summary
            };

            _logger.LogDebug("Capacity trends generated with {DataPointsCount} data points", dataPoints.Count);
            return trendsDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing capacity trends query");
            throw;
        }
    }

    private List<(DateTime Start, DateTime End)> GenerateTimePeriods(DateTime startDate, DateTime endDate, string groupBy)
    {
        var periods = new List<(DateTime Start, DateTime End)>();
        var current = startDate.Date; // Start at midnight

        switch (groupBy.ToLower())
        {
            case "hour":
                current = new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, 0, 0);
                while (current <= endDate)
                {
                    periods.Add((current, current.AddHours(1)));
                    current = current.AddHours(1);
                }
                break;

            case "week":
                // Start at beginning of week (Monday)
                var daysToMonday = (int)current.DayOfWeek - 1;
                if (daysToMonday < 0) daysToMonday = 6;
                current = current.AddDays(-daysToMonday);
                
                while (current <= endDate)
                {
                    periods.Add((current, current.AddDays(7)));
                    current = current.AddDays(7);
                }
                break;

            default: // "day"
                while (current <= endDate)
                {
                    periods.Add((current, current.AddDays(1)));
                    current = current.AddDays(1);
                }
                break;
        }

        return periods;
    }

    private string FormatPeriodLabel(DateTime dateTime, string groupBy)
    {
        return groupBy.ToLower() switch
        {
            "hour" => dateTime.ToString("MMM dd, HH:00"),
            "week" => $"Week of {dateTime:MMM dd, yyyy}",
            _ => dateTime.ToString("MMM dd, yyyy")
        };
    }
}
