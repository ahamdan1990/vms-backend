namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class CdOccupantDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsStaff { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? EntryMethod { get; set; }
    public int? CameraId { get; set; }
    public string? CameraName { get; set; }
    public int? InvitationId { get; set; }
    public int? AttendanceId { get; set; }
    public bool IsCurrentlyInside => CheckOutTime == null;
}
