using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for time slot management operations
/// </summary>
[ApiController]
[Route("api/time-slots")]
[Authorize]
public class TimeSlotsController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TimeSlotsController> _logger;

    public TimeSlotsController(IUnitOfWork unitOfWork, ILogger<TimeSlotsController> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all time slots with optional filtering
    /// </summary>
    /// <param name="locationId">Filter by location ID</param>
    /// <param name="activeOnly">Only return active time slots</param>
    /// <returns>List of time slots</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.Invitation.ReadOwn)]
    public async Task<IActionResult> GetTimeSlots(
        [FromQuery] int? locationId = null,
        [FromQuery] bool activeOnly = true)
    {
        try
        {
            var query = _unitOfWork.Repository<TimeSlot>().GetQueryable();

            if (activeOnly)
            {
                query = query.Where(ts => ts.IsActive);
            }

            if (locationId.HasValue)
            {
                query = query.Where(ts => ts.LocationId == null || ts.LocationId == locationId.Value);
            }

            var timeSlots = await query
                .OrderBy(ts => ts.DisplayOrder)
                .ThenBy(ts => ts.StartTime)
                .ToListAsync();

            var timeSlotDtos = timeSlots.Select(ts => new TimeSlotDto
            {
                Id = ts.Id,
                Name = ts.Name,
                StartTime = ts.StartTime,
                EndTime = ts.EndTime,
                MaxVisitors = ts.MaxVisitors,
                ActiveDays = ts.ActiveDays,
                LocationId = ts.LocationId,
                LocationName = ts.Location?.Name,
                IsActive = ts.IsActive,
                BufferMinutes = ts.BufferMinutes,
                DurationMinutes = ts.DurationMinutes,
                DisplayOrder = ts.DisplayOrder
            }).ToList();

            return SuccessResponse(timeSlotDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time slots");
            return BadRequestResponse("Failed to retrieve time slots");
        }
    }

    /// <summary>
    /// Creates a new time slot
    /// </summary>
    /// <param name="createDto">Time slot creation data</param>
    /// <returns>Created time slot</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.SystemConfig.ManageCapacity)]
    public async Task<IActionResult> CreateTimeSlot([FromBody] CreateTimeSlotDto createDto)
    {
        try
        {
            var timeSlot = new TimeSlot
            {
                Name = createDto.Name.Trim(),
                StartTime = createDto.StartTime,
                EndTime = createDto.EndTime,
                MaxVisitors = createDto.MaxVisitors,
                ActiveDays = createDto.ActiveDays.Trim(),
                LocationId = createDto.LocationId,
                BufferMinutes = createDto.BufferMinutes,
                DisplayOrder = createDto.DisplayOrder,
                IsActive = true
            };

            timeSlot.SetCreatedBy(GetCurrentUserId() ?? 1);

            var validationErrors = timeSlot.ValidateTimeSlot();
            if (validationErrors.Any())
            {
                return BadRequestResponse($"Validation failed: {string.Join(", ", validationErrors)}");
            }

            await _unitOfWork.Repository<TimeSlot>().AddAsync(timeSlot);
            await _unitOfWork.SaveChangesAsync();

            var timeSlotDto = new TimeSlotDto
            {
                Id = timeSlot.Id,
                Name = timeSlot.Name,
                StartTime = timeSlot.StartTime,
                EndTime = timeSlot.EndTime,
                MaxVisitors = timeSlot.MaxVisitors,
                ActiveDays = timeSlot.ActiveDays,
                LocationId = timeSlot.LocationId,
                IsActive = timeSlot.IsActive,
                BufferMinutes = timeSlot.BufferMinutes,
                DurationMinutes = timeSlot.DurationMinutes,
                DisplayOrder = timeSlot.DisplayOrder
            };

            return CreatedResponse(timeSlotDto, Url.Action(nameof(GetTimeSlot), new { id = timeSlot.Id }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating time slot");
            return BadRequestResponse("Failed to create time slot");
        }
    }

    /// <summary>
    /// Gets a specific time slot by ID
    /// </summary>
    /// <param name="id">Time slot ID</param>
    /// <returns>Time slot details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.Invitation.ReadOwn)]
    public async Task<IActionResult> GetTimeSlot(int id)
    {
        try
        {
            var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetByIdAsync(id);
            if (timeSlot == null)
            {
                return NotFoundResponse("Time slot", id);
            }

            var timeSlotDto = new TimeSlotDto
            {
                Id = timeSlot.Id,
                Name = timeSlot.Name,
                StartTime = timeSlot.StartTime,
                EndTime = timeSlot.EndTime,
                MaxVisitors = timeSlot.MaxVisitors,
                ActiveDays = timeSlot.ActiveDays,
                LocationId = timeSlot.LocationId,
                LocationName = timeSlot.Location?.Name,
                IsActive = timeSlot.IsActive,
                BufferMinutes = timeSlot.BufferMinutes,
                DurationMinutes = timeSlot.DurationMinutes,
                DisplayOrder = timeSlot.DisplayOrder
            };

            return SuccessResponse(timeSlotDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time slot {Id}", id);
            return BadRequestResponse("Failed to retrieve time slot");
        }
    }
}
