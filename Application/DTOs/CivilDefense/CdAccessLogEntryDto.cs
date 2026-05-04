namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class CdAccessLogEntryDto
{
    public int Id { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? NationalId { get; set; }
    public bool IsStaff { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? EntryMethod { get; set; }
    public string? CameraName { get; set; }
    public string? Notes { get; set; }
}
