using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Capacity;

/// <summary>
/// DTO for time slot information
/// </summary>
public class TimeSlotDto
{
    /// <summary>
    /// Time slot ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Time slot name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Start time
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// Maximum visitors
    /// </summary>
    public int MaxVisitors { get; set; }

    /// <summary>
    /// Active days
    /// </summary>
    public string ActiveDays { get; set; } = string.Empty;

    /// <summary>
    /// Location ID (if specific to a location)
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Location name
    /// </summary>
    public string? LocationName { get; set; }

    /// <summary>
    /// Whether the slot is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Buffer minutes
    /// </summary>
    public int BufferMinutes { get; set; }

    /// <summary>
    /// Duration in minutes
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for creating a time slot
/// </summary>
public class CreateTimeSlotDto
{
    /// <summary>
    /// Time slot name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Start time
    /// </summary>
    [Required]
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    [Required]
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// Maximum visitors
    /// </summary>
    [Range(1, 10000)]
    public int MaxVisitors { get; set; } = 50;

    /// <summary>
    /// Active days (1=Monday, 7=Sunday)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ActiveDays { get; set; } = "1,2,3,4,5";

    /// <summary>
    /// Location ID (optional)
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Buffer minutes
    /// </summary>
    [Range(0, 60)]
    public int BufferMinutes { get; set; } = 15;

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; } = 1;
}

