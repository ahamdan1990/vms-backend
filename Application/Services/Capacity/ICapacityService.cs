using VisitorManagementSystem.Api.Application.DTOs.Capacity;

namespace VisitorManagementSystem.Api.Application.Services.Capacity;

/// <summary>
/// Interface for capacity management services
/// </summary>
public interface ICapacityService
{
    /// <summary>
    /// Validates if capacity is available for a new invitation
    /// </summary>
    /// <param name="request">Capacity validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Capacity validation response</returns>
    Task<CapacityValidationResponseDto> ValidateCapacityAsync(
        CapacityValidationRequestDto request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current occupancy for a specific date/time/location
    /// </summary>
    /// <param name="dateTime">Date and time</param>
    /// <param name="locationId">Location ID (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current occupancy count</returns>
    Task<int> GetCurrentOccupancyAsync(
        DateTime dateTime, 
        int? locationId = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets maximum capacity for a specific date/time/location
    /// </summary>
    /// <param name="dateTime">Date and time</param>
    /// <param name="locationId">Location ID (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Maximum capacity</returns>
    Task<int> GetMaxCapacityAsync(
        DateTime dateTime, 
        int? locationId = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alternative time slots when capacity is unavailable
    /// </summary>
    /// <param name="originalDateTime">Original requested date/time</param>
    /// <param name="expectedVisitors">Number of expected visitors</param>
    /// <param name="locationId">Location ID (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of alternative time slots</returns>
    Task<List<AlternativeTimeSlotDto>> GetAlternativeTimeSlotsAsync(
        DateTime originalDateTime,
        int expectedVisitors,
        int? locationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates occupancy when invitation status changes
    /// </summary>
    /// <param name="invitationId">Invitation ID</param>
    /// <param name="oldStatus">Previous status</param>
    /// <param name="newStatus">New status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UpdateOccupancyForInvitationAsync(
        int invitationId,
        string oldStatus,
        string newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets occupancy statistics for a date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="locationId">Location ID (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Occupancy statistics</returns>
    Task<object> GetOccupancyStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        int? locationId = null,
        CancellationToken cancellationToken = default);
}
