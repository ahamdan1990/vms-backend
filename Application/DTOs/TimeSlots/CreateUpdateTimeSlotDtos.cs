namespace VisitorManagementSystem.Api.Application.DTOs.TimeSlots;

/// <summary>
/// DTO for creating a time slot
/// </summary>
public class CreateTimeSlotDto
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxVisitors { get; set; }
    public string ActiveDays { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public int BufferMinutes { get; set; } = 0;
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// DTO for updating a time slot
/// </summary>
public class UpdateTimeSlotDto
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxVisitors { get; set; }
    public string ActiveDays { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public int BufferMinutes { get; set; } = 0;
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO for available time slot
/// </summary>
public class AvailableTimeSlotDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxVisitors { get; set; }
    public int CurrentBookings { get; set; }
    public int AvailableSlots { get; set; }
    public bool IsAvailable { get; set; }
    public string? LocationName { get; set; }
}