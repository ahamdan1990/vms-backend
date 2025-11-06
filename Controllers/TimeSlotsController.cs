using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.TimeSlots;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;
using VisitorManagementSystem.Api.Application.Queries.TimeSlots;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for time slot management operations
/// </summary>
[ApiController]
[Route("api/time-slots")]
[Authorize]
public class TimeSlotsController : BaseController
{
    private readonly IMediator _mediator;

    public TimeSlotsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets all time slots with optional filtering
    /// </summary>
    /// <param name="locationId">Filter by location ID</param>
    /// <param name="activeOnly">Only return active time slots</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction</param>
    /// <returns>List of time slots</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.Invitation.Read)]
    public async Task<IActionResult> GetTimeSlots(
        [FromQuery] int? locationId = null,
        [FromQuery] bool activeOnly = true,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortBy = "DisplayOrder",
        [FromQuery] string sortDirection = "asc")
    {
        var query = new GetTimeSlotsQuery
        {
            LocationId = locationId,
            ActiveOnly = activeOnly,
            PageIndex = pageIndex,
            PageSize = Math.Min(pageSize, 100),
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets a specific time slot by ID
    /// </summary>
    /// <param name="id">Time slot ID</param>
    /// <returns>Time slot details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.Invitation.Read)]
    public async Task<IActionResult> GetTimeSlot(int id)
    {
        var query = new GetTimeSlotByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFoundResponse("Time slot", id);
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Creates a new time slot
    /// </summary>
    /// <param name="createDto">Time slot creation data</param>
    /// <returns>Created time slot</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> CreateTimeSlot([FromBody] CreateTimeSlotDto createDto)
    {
        var command = new CreateTimeSlotCommand
        {
            Name = createDto.Name,
            StartTime = createDto.StartTime,
            EndTime = createDto.EndTime,
            MaxVisitors = createDto.MaxVisitors,
            ActiveDays = createDto.ActiveDays,
            LocationId = createDto.LocationId,
            BufferMinutes = createDto.BufferMinutes,
            DisplayOrder = createDto.DisplayOrder,
            CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return CreatedResponse(result, Url.Action(nameof(GetTimeSlot), new { id = result.Id }));
    }

    /// <summary>
    /// Updates an existing time slot
    /// </summary>
    /// <param name="id">Time slot ID</param>
    /// <param name="updateDto">Time slot update data</param>
    /// <returns>Updated time slot</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> UpdateTimeSlot(int id, [FromBody] UpdateTimeSlotDto updateDto)
    {
        var command = new UpdateTimeSlotCommand
        {
            Id = id,
            Name = updateDto.Name,
            StartTime = updateDto.StartTime,
            EndTime = updateDto.EndTime,
            MaxVisitors = updateDto.MaxVisitors,
            ActiveDays = updateDto.ActiveDays,
            LocationId = updateDto.LocationId,
            BufferMinutes = updateDto.BufferMinutes,
            DisplayOrder = updateDto.DisplayOrder,
            IsActive = updateDto.IsActive,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Deletes a time slot (soft delete)
    /// </summary>
    /// <param name="id">Time slot ID</param>
    /// <param name="hardDelete">Whether to perform hard delete</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> DeleteTimeSlot(int id, [FromQuery] bool hardDelete = false)
    {
        var command = new DeleteTimeSlotCommand
        {
            Id = id,
            HardDelete = hardDelete,
            DeletedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result, "Time slot deleted successfully");
    }

    /// <summary>
    /// Gets available time slots for a specific date and location
    /// </summary>
    /// <param name="date">Date</param>
    /// <param name="locationId">Location ID</param>
    /// <returns>Available time slots</returns>
    [HttpGet("available")]
    [Authorize(Policy = Permissions.Invitation.Read)]
    public async Task<IActionResult> GetAvailableTimeSlots(
        [FromQuery] DateTime date,
        [FromQuery] int? locationId = null)
    {
        var query = new GetAvailableTimeSlotsQuery
        {
            Date = date,
            LocationId = locationId
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }
}