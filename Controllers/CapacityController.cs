using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;
using VisitorManagementSystem.Api.Application.Queries.Capacity;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for capacity management operations
/// </summary>
[ApiController]
[Route("api/capacity")]
[Authorize]
public class CapacityController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<CapacityController> _logger;

    public CapacityController(IMediator mediator, ILogger<CapacityController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var query = new ValidateCapacityQuery
            {
                LocationId = locationId,
                DateTime = dateTime,
                ExpectedVisitors = 0 // Just checking current state
            };

            var result = await _mediator.Send(query);
            
            var occupancyInfo = new
            {
                DateTime = dateTime,
                LocationId = locationId,
                CurrentOccupancy = result.CurrentOccupancy,
                MaxCapacity = result.MaxCapacity,
                AvailableSlots = result.AvailableSlots,
                OccupancyPercentage = result.OccupancyPercentage,
                IsWarningLevel = result.IsWarningLevel
            };

            return SuccessResponse(occupancyInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting occupancy for {DateTime} at location {LocationId}", 
                dateTime, locationId);
            return BadRequestResponse("Failed to get occupancy information");
        }
    }

    /// <summary>
    /// Gets capacity statistics for a date range
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
            // For now, return basic statistics
            // This would be enhanced with proper analytics service
            var statistics = new
            {
                StartDate = startDate,
                EndDate = endDate,
                LocationId = locationId,
                Message = "Capacity statistics endpoint - implementation in progress"
            };

            return SuccessResponse(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting capacity statistics for {StartDate} to {EndDate}", 
                startDate, endDate);
            return BadRequestResponse("Failed to get capacity statistics");
        }
    }
}
