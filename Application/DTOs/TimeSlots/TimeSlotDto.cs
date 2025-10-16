namespace VisitorManagementSystem.Api.Application.DTOs.TimeSlots;

/// <summary>
/// DTO for time slot data
/// </summary>
public class TimeSlotDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxVisitors { get; set; }
    public string ActiveDays { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public bool IsActive { get; set; }
    public int BufferMinutes { get; set; }
    public int DurationMinutes { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}