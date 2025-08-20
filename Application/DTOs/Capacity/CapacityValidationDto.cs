using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Capacity;

/// <summary>
/// DTO for capacity validation request
/// </summary>
public class CapacityValidationRequestDto
{
    /// <summary>
    /// Location ID to check capacity for
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Time slot ID to check capacity for
    /// </summary>
    public int? TimeSlotId { get; set; }

    /// <summary>
    /// Date and time for the appointment
    /// </summary>
    [Required]
    public DateTime DateTime { get; set; }

    /// <summary>
    /// Number of expected visitors
    /// </summary>
    [Range(1, 1000)]
    public int ExpectedVisitors { get; set; } = 1;

    /// <summary>
    /// Whether this is a VIP request (can override capacity)
    /// </summary>
    public bool IsVipRequest { get; set; } = false;

    /// <summary>
    /// Invitation ID to exclude from capacity calculation (for updates)
    /// </summary>
    public int? ExcludeInvitationId { get; set; }
}

/// <summary>
/// DTO for capacity validation response
/// </summary>
public class CapacityValidationResponseDto
{
    /// <summary>
    /// Whether capacity is available
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Current occupancy count
    /// </summary>
    public int CurrentOccupancy { get; set; }

    /// <summary>
    /// Maximum capacity
    /// </summary>
    public int MaxCapacity { get; set; }

    /// <summary>
    /// Available slots remaining
    /// </summary>
    public int AvailableSlots { get; set; }

    /// <summary>
    /// Occupancy percentage
    /// </summary>
    public decimal OccupancyPercentage { get; set; }

    /// <summary>
    /// Warning level reached (>80% capacity)
    /// </summary>
    public bool IsWarningLevel { get; set; }

    /// <summary>
    /// Validation messages
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// Alternative time slots if current is unavailable
    /// </summary>
    public List<AlternativeTimeSlotDto> AlternativeSlots { get; set; } = new();
}
