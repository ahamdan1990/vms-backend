using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.TimeSlots;

/// <summary>
/// DTO for time slot booking data
/// </summary>
public class TimeSlotBookingDto
{
    public int Id { get; set; }
    public int TimeSlotId { get; set; }
    public string TimeSlotName { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public int? InvitationId { get; set; }
    public string? InvitationNumber { get; set; }
    public int VisitorCount { get; set; }
    public BookingStatus Status { get; set; }
    public string? Notes { get; set; }
    public int BookedBy { get; set; }
    public string BookedByName { get; set; } = string.Empty;
    public DateTime BookedOn { get; set; }
    public DateTime? CancelledOn { get; set; }
    public int? CancelledBy { get; set; }
    public string? CancelledByName { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

/// <summary>
/// DTO for creating a time slot booking
/// </summary>
public class CreateTimeSlotBookingDto
{
    public int TimeSlotId { get; set; }
    public DateTime BookingDate { get; set; }
    public int? InvitationId { get; set; }
    public int VisitorCount { get; set; } = 1;
    public string? Notes { get; set; }
}
