using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Capacity;

/// <summary>
/// Service for managing facility capacity and occupancy
/// </summary>
public class CapacityService : ICapacityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CapacityService> _logger;

    public CapacityService(IUnitOfWork unitOfWork, ILogger<CapacityService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates if capacity is available for a new invitation
    /// </summary>
    public async Task<CapacityValidationResponseDto> ValidateCapacityAsync(
        CapacityValidationRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating capacity for {ExpectedVisitors} visitors at {DateTime} for location {LocationId}",
                request.ExpectedVisitors, request.DateTime, request.LocationId);

            var response = new CapacityValidationResponseDto();

            // Get max capacity for the requested date/time/location
            var maxCapacity = await GetMaxCapacityAsync(request.DateTime, request.LocationId, cancellationToken);
            response.MaxCapacity = maxCapacity;

            // Get current occupancy
            var currentOccupancy = await GetCurrentOccupancyAsync(
                request.DateTime, request.LocationId, cancellationToken);
            response.CurrentOccupancy = currentOccupancy;

            // Calculate available slots
            response.AvailableSlots = maxCapacity - currentOccupancy;
            response.OccupancyPercentage = maxCapacity > 0 ? 
                Math.Round((decimal)currentOccupancy / maxCapacity * 100, 2) : 0;
            response.IsWarningLevel = response.OccupancyPercentage >= 80;

            // Check if capacity is available
            response.IsAvailable = response.AvailableSlots >= request.ExpectedVisitors;

            // VIP override logic
            if (!response.IsAvailable && request.IsVipRequest)
            {
                response.IsAvailable = true;
                response.Messages.Add("VIP override applied - capacity limit bypassed");
                _logger.LogInformation("VIP override applied for capacity validation at {DateTime}", request.DateTime);
            }

            // Add validation messages
            if (!response.IsAvailable)
            {
                response.Messages.Add($"Insufficient capacity: {request.ExpectedVisitors} visitors requested, " +
                    $"only {response.AvailableSlots} slots available");
                
                // Get alternative time slots
                response.AlternativeSlots = await GetAlternativeTimeSlotsAsync(
                    request.DateTime, request.ExpectedVisitors, request.LocationId, cancellationToken);
            }
            else if (response.IsWarningLevel)
            {
                response.Messages.Add($"Warning: Facility is at {response.OccupancyPercentage}% capacity");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating capacity for {ExpectedVisitors} visitors at {DateTime}",
                request.ExpectedVisitors, request.DateTime);
            throw;
        }
    }

    /// <summary>
    /// Gets current occupancy for a specific date/time/location
    /// </summary>
    public async Task<int> GetCurrentOccupancyAsync(
        DateTime dateTime, 
        int? locationId = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Count approved invitations for the given date/time/location
            var query = _unitOfWork.Invitations.GetQueryable()
                .Where(i => !i.IsDeleted &&
                           i.Status == InvitationStatus.Approved &&
                           i.ScheduledStartTime.Date == dateTime.Date);

            // Apply location filter if specified
            if (locationId.HasValue)
            {
                query = query.Where(i => i.LocationId == locationId.Value);
            }

            // Filter by time overlap
            query = query.Where(i => 
                i.ScheduledStartTime <= dateTime && 
                i.ScheduledEndTime >= dateTime);

            var invitations = await query.ToListAsync(cancellationToken);
            var totalVisitors = invitations.Sum(i => i.ExpectedVisitorCount);

            _logger.LogDebug("Current occupancy: {TotalVisitors} visitors from {InvitationCount} invitations at {DateTime}",
                totalVisitors, invitations.Count, dateTime);

            return totalVisitors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current occupancy for {DateTime} at location {LocationId}",
                dateTime, locationId);
            throw;
        }
    }

    /// <summary>
    /// Gets maximum capacity for a specific date/time/location
    /// </summary>
    public async Task<int> GetMaxCapacityAsync(
        DateTime dateTime, 
        int? locationId = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var capacity = 0;

            // First, try to get capacity from time slot
            var timeSlot = await GetActiveTimeSlotForDateTime(dateTime, locationId, cancellationToken);
            if (timeSlot != null)
            {
                capacity = timeSlot.MaxVisitors;
                _logger.LogDebug("Found time slot capacity: {Capacity} for slot {TimeSlotName}", 
                    capacity, timeSlot.Name);
            }

            // If no time slot found or location-specific, check location capacity
            if (locationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(locationId.Value, cancellationToken);
                if (location != null)
                {
                    // Use smaller of time slot or location capacity
                    var locationCapacity = location.MaxOccupancy;
                    capacity = timeSlot != null ? Math.Min(capacity, locationCapacity) : locationCapacity;
                    
                    _logger.LogDebug("Location capacity: {LocationCapacity}, final capacity: {FinalCapacity}",
                        locationCapacity, capacity);
                }
            }

            // Default capacity if nothing found
            if (capacity == 0)
            {
                capacity = 100; // Default system capacity
                _logger.LogWarning("No specific capacity found for {DateTime} at location {LocationId}, using default: {Capacity}",
                    dateTime, locationId, capacity);
            }

            return capacity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting max capacity for {DateTime} at location {LocationId}",
                dateTime, locationId);
            throw;
        }
    }

    /// <summary>
    /// Gets active time slot for a specific date/time
    /// </summary>
    private async Task<TimeSlot?> GetActiveTimeSlotForDateTime(
        DateTime dateTime, 
        int? locationId, 
        CancellationToken cancellationToken)
    {
        var timeSlots = await _unitOfWork.Repository<TimeSlot>()
            .GetQueryable()
            .Where(ts => !ts.IsDeleted && ts.IsActive)
            .Where(ts => locationId == null || ts.LocationId == null || ts.LocationId == locationId)
            .ToListAsync(cancellationToken);

        return timeSlots.FirstOrDefault(ts => 
            ts.ContainsTime(dateTime) && ts.IsActiveOnDay(dateTime.DayOfWeek));
    }

    /// <summary>
    /// Gets alternative time slots when capacity is unavailable
    /// </summary>
    public async Task<List<AlternativeTimeSlotDto>> GetAlternativeTimeSlotsAsync(
        DateTime originalDateTime,
        int expectedVisitors,
        int? locationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alternatives = new List<AlternativeTimeSlotDto>();
            var searchDate = originalDateTime.Date;

            // Search for alternatives within next 7 days
            for (int day = 0; day < 7; day++)
            {
                var checkDate = searchDate.AddDays(day);
                
                var timeSlots = await _unitOfWork.Repository<TimeSlot>()
                    .GetQueryable()
                    .Where(ts => !ts.IsDeleted && ts.IsActive)
                    .Where(ts => locationId == null || ts.LocationId == null || ts.LocationId == locationId)
                    .ToListAsync(cancellationToken);

                foreach (var timeSlot in timeSlots.Where(ts => ts.IsActiveOnDay(checkDate.DayOfWeek)))
                {
                    var slotDateTime = checkDate.Add(timeSlot.StartTime.ToTimeSpan());
                    
                    // Skip if it's the same as original request
                    if (slotDateTime == originalDateTime) continue;
                    
                    // Skip if in the past
                    if (slotDateTime < DateTime.Now) continue;

                    var occupancy = await GetCurrentOccupancyAsync(slotDateTime, locationId, cancellationToken);
                    var maxCapacity = timeSlot.MaxVisitors;
                    var availableCapacity = maxCapacity - occupancy;

                    if (availableCapacity >= expectedVisitors)
                    {
                        alternatives.Add(new AlternativeTimeSlotDto
                        {
                            TimeSlotId = timeSlot.Id,
                            Name = timeSlot.Name,
                            DateTime = slotDateTime,
                            AvailableCapacity = availableCapacity,
                            OccupancyPercentage = maxCapacity > 0 ? 
                                Math.Round((decimal)occupancy / maxCapacity * 100, 2) : 0
                        });
                    }
                }

                // Limit to 5 alternatives
                if (alternatives.Count >= 5) break;
            }

            return alternatives.OrderBy(a => a.DateTime).Take(5).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alternative time slots for {DateTime}", originalDateTime);
            return new List<AlternativeTimeSlotDto>();
        }
    }

    /// <summary>
    /// Updates occupancy when invitation status changes
    /// </summary>
    public async Task UpdateOccupancyForInvitationAsync(
        int invitationId,
        string oldStatus,
        string newStatus,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating occupancy for invitation {InvitationId}: {OldStatus} -> {NewStatus}",
                invitationId, oldStatus, newStatus);

            // This method can be used to update occupancy logs when invitation statuses change
            // For now, we calculate occupancy on-demand, but this could be optimized with caching
            
            var invitation = await _unitOfWork.Invitations.GetByIdAsync(invitationId, cancellationToken);
            if (invitation == null) return;

            // Log the status change for audit purposes
            _logger.LogInformation("Invitation {InvitationId} status changed from {OldStatus} to {NewStatus}, " +
                "affecting capacity for {ExpectedVisitors} visitors at {DateTime}",
                invitationId, oldStatus, newStatus, invitation.ExpectedVisitorCount, invitation.ScheduledStartTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating occupancy for invitation {InvitationId}", invitationId);
            throw;
        }
    }

    /// <summary>
    /// Gets occupancy statistics for a date range
    /// </summary>
    public async Task<object> GetOccupancyStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        int? locationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _unitOfWork.Invitations.GetQueryable()
                .Where(i => !i.IsDeleted &&
                           i.Status == InvitationStatus.Approved &&
                           i.ScheduledStartTime.Date >= startDate.Date &&
                           i.ScheduledStartTime.Date <= endDate.Date);

            if (locationId.HasValue)
            {
                query = query.Where(i => i.LocationId == locationId.Value);
            }

            var invitations = await query.ToListAsync(cancellationToken);

            // Handle empty data gracefully
            var dailyGroups = invitations.GroupBy(i => i.ScheduledStartTime.Date).ToList();
            
            var stats = new
            {
                TotalInvitations = invitations.Count,
                TotalVisitors = invitations.Sum(i => i.ExpectedVisitorCount),
                AverageVisitorsPerDay = dailyGroups.Any() 
                    ? dailyGroups.Average(g => g.Sum(i => i.ExpectedVisitorCount))
                    : 0,
                PeakDay = dailyGroups
                    .OrderByDescending(g => g.Sum(i => i.ExpectedVisitorCount))
                    .FirstOrDefault()?.Key,
                DailyBreakdown = dailyGroups
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.ExpectedVisitorCount))
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting occupancy statistics for {StartDate} to {EndDate}", 
                startDate, endDate);
            throw;
        }
    }
}
