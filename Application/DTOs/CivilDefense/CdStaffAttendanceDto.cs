using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class CdStaffAttendanceDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public AttendanceMethod Method { get; set; }
    public int? CameraId { get; set; }
    public string? CameraName { get; set; }
    public string? Notes { get; set; }
    public bool IsCurrentlyInside { get; set; }
    public TimeSpan? Duration { get; set; }
}
