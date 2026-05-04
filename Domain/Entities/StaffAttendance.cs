using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Records when a staff member enters or exits the building.
/// Staff members do not have Invitation records, so this is the sole
/// attendance tracking mechanism for Civil Defense live-building views.
/// </summary>
public class StaffAttendance : AuditableEntity
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public DateTime CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    [Required]
    public AttendanceMethod Method { get; set; } = AttendanceMethod.Manual;

    public int? CameraId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Camera? Camera { get; set; }

    // Business helpers
    public bool IsCurrentlyInside => CheckOutTime == null;

    public TimeSpan? Duration => CheckOutTime.HasValue
        ? CheckOutTime.Value - CheckInTime
        : DateTime.UtcNow - CheckInTime;

    public void RecordCheckOut(int processedBy)
    {
        CheckOutTime = DateTime.UtcNow;
        UpdateModifiedBy(processedBy);
    }
}
