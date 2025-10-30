using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;
using VisitorManagementSystem.Api.Application.Queries.Capacity;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for capacity monitoring and validation operations
/// Focuses on monitoring existing capacity rather than CRUD operations
/// </summary>
[ApiController]
[Route("api/capacity")]
[Authorize]
public class CapacityController : BaseController
{
    private readonly IMediator _mediator;

    public CapacityController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Validates capacity for a specific date/time/location
    /// </summary>
    /// <param name="locationId">Location ID (optional)</param>
    /// <param name="timeSlotId">Time slot ID (optional)</param>
    /// <param name="dateTime">Date and time</param>
    /// <param name="expectedVisitors">Expected number of visitors</param>
    /// <param name="isVipRequest">Whether this is a VIP request</param>
    /// <param name="excludeInvitationId">Invitation ID to exclude (for updates)</param>
    /// <returns>Capacity validation result</returns>
    [HttpGet("validate")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> ValidateCapacity(
        [FromQuery] int? locationId = null,
        [FromQuery] int? timeSlotId = null,
        [FromQuery, Required] DateTime dateTime = default,
        [FromQuery, Range(1, 1000)] int expectedVisitors = 1,
        [FromQuery] bool isVipRequest = false,
        [FromQuery] int? excludeInvitationId = null)
    {
        if (dateTime == default)
        {
            return BadRequestResponse("DateTime is required");
        }

        if (dateTime < DateTime.Now.AddMinutes(-5)) // Allow 5 minute buffer for current time
        {
            return BadRequestResponse("Cannot validate capacity for past dates");
        }

        var query = new ValidateCapacityQuery
        {
            LocationId = locationId,
            TimeSlotId = timeSlotId,
            DateTime = dateTime,
            ExpectedVisitors = expectedVisitors,
            IsVipRequest = isVipRequest,
            ExcludeInvitationId = excludeInvitationId
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets current occupancy for a specific date/time/location
    /// </summary>
    /// <param name="dateTime">Date and time</param>
    /// <param name="locationId">Location ID (optional)</param>
    /// <returns>Current occupancy information</returns>
    [HttpGet("occupancy")]
    [Authorize(Policy = Permissions.Dashboard.ViewBasic)]
    public async Task<IActionResult> GetOccupancy(
        [FromQuery, Required] DateTime dateTime,
        [FromQuery] int? locationId = null)
    {
        if (dateTime == default)
        {
            return BadRequestResponse("DateTime is required");
        }

        try
        {
            var query = new GetOccupancyQuery
            {
                DateTime = dateTime,
                LocationId = locationId
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            return BadRequestResponse($"Failed to get occupancy information: {ex.Message}");
        }
    }
    /// <summary>
    /// Gets occupancy statistics for a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="locationId">Location ID (optional)</param>
    /// <returns>Capacity statistics</returns>
    [HttpGet("statistics")]
    [Authorize(Policy = Permissions.Report.GenerateOwn)]
    public async Task<IActionResult> GetCapacityStatistics(
        [FromQuery, Required] DateTime startDate,
        [FromQuery, Required] DateTime endDate,
        [FromQuery] int? locationId = null)
    {
        if (startDate == default || endDate == default)
        {
            return BadRequestResponse("Start date and end date are required");
        }

        if (startDate > endDate)
        {
            return BadRequestResponse("Start date cannot be after end date");
        }

        if (endDate > DateTime.Now.AddDays(30))
        {
            return BadRequestResponse("End date cannot be more than 30 days in the future");
        }

        try
        {
            var query = new GetCapacityStatisticsQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                LocationId = locationId
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            return BadRequestResponse($"Failed to get capacity statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets alternative time slots when capacity is unavailable
    /// </summary>
    /// <param name="originalDateTime">Original requested date/time</param>
    /// <param name="expectedVisitors">Number of expected visitors</param>
    /// <param name="locationId">Location ID (optional)</param>
    /// <returns>Alternative time slots</returns>
    [HttpGet("alternatives")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> GetAlternativeTimeSlots(
        [FromQuery, Required] DateTime originalDateTime,
        [FromQuery, Range(1, 1000)] int expectedVisitors = 1,
        [FromQuery] int? locationId = null)
    {
        if (originalDateTime == default)
        {
            return BadRequestResponse("Original date time is required");
        }

        try
        {
            var query = new GetAlternativeTimeSlotsQuery
            {
                OriginalDateTime = originalDateTime,
                ExpectedVisitors = expectedVisitors,
                LocationId = locationId
            };

            var result = await _mediator.Send(query);
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            return BadRequestResponse($"Failed to get alternative time slots: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets real-time capacity overview for multiple locations
    /// </summary>
    /// <param name="dateTime">Date and time to check</param>
    /// <param name="locationIds">Specific location IDs (optional)</param>
    /// <param name="includeInactive">Include inactive locations</param>
    /// <param name="locationType">Filter by location type</param>
    /// <returns>Capacity overview for all/specified locations</returns>
    [HttpGet("overview")]
    [Authorize(Policy = Permissions.Dashboard.ViewBasic)]
    public async Task<IActionResult> GetCapacityOverview(
        [FromQuery] DateTime? dateTime = null,
        [FromQuery] int[]? locationIds = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? locationType = null)
    {
        try
        {
            var query = new GetCapacityOverviewQuery
            {
                DateTime = dateTime ?? DateTime.Now,
                LocationIds = locationIds,
                IncludeInactive = includeInactive
            };

            var overview = await _mediator.Send(query);

            // Transform to frontend-compatible format
            var frontendOverview = overview.Select(loc => new FrontendCapacityOverviewDto
            {
                Id = loc.LocationId,
                LocationId = loc.LocationId,
                Name = loc.LocationName,
                LocationName = loc.LocationName,
                MaxCapacity = loc.MaxCapacity,
                CurrentOccupancy = loc.CurrentOccupancy,
                AvailableSlots = loc.AvailableSlots,
                OccupancyPercentage = loc.OccupancyPercentage,
                IsAtCapacity = loc.IsAtCapacity,
                IsWarningLevel = loc.IsWarningLevel
            }).ToList();

            return SuccessResponse(frontendOverview);
        }
        catch (Exception ex)
        {
            return BadRequestResponse($"Failed to get capacity overview: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets capacity utilization trends for monitoring
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="locationId">Location ID (optional)</param>
    /// <param name="groupBy">Group by period (hour, day, week)</param>
    /// <returns>Capacity utilization trends</returns>
    [HttpGet("trends")]
    [Authorize(Policy = Permissions.Report.GenerateOwn)]
    public async Task<IActionResult> GetCapacityTrends(
        [FromQuery, Required] DateTime startDate,
        [FromQuery, Required] DateTime endDate,
        [FromQuery] int? locationId = null,
        [FromQuery] string groupBy = "day")
    {
        if (startDate == default || endDate == default)
        {
            return BadRequestResponse("Start date and end date are required");
        }

        if (startDate > endDate)
        {
            return BadRequestResponse("Start date cannot be after end date");
        }

        var validGroupBy = new[] { "hour", "day", "week" };
        if (!validGroupBy.Contains(groupBy.ToLower()))
        {
            return BadRequestResponse($"Invalid groupBy value. Must be one of: {string.Join(", ", validGroupBy)}");
        }

        try
        {
            var query = new GetCapacityTrendsQuery
            {
                StartDate = startDate,
                EndDate = endDate,
                LocationId = locationId,
                GroupBy = groupBy
            };

            var trends = await _mediator.Send(query);
            return SuccessResponse(trends);
        }
        catch (Exception ex)
        {
            return BadRequestResponse($"Failed to get capacity trends: {ex.Message}");
        }
    }
}