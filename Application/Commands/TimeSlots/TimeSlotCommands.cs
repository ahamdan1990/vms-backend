using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;

namespace VisitorManagementSystem.Api.Application.Commands.TimeSlots;

/// <summary>
/// Command to create a new time slot
/// </summary>
public class CreateTimeSlotCommand : IRequest<TimeSlotDto>
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxVisitors { get; set; }
    public string ActiveDays { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public int BufferMinutes { get; set; }
    public int DisplayOrder { get; set; }
    public int CreatedBy { get; set; }
}

/// <summary>
/// Command to update an existing time slot
/// </summary>
public class UpdateTimeSlotCommand : IRequest<TimeSlotDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxVisitors { get; set; }
    public string ActiveDays { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public int BufferMinutes { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int ModifiedBy { get; set; }
}

/// <summary>
/// Command to delete a time slot
/// </summary>
public class DeleteTimeSlotCommand : IRequest<bool>
{
    public int Id { get; set; }
    public bool HardDelete { get; set; }
    public int DeletedBy { get; set; }
}