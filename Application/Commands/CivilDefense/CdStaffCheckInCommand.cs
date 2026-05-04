using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class CdStaffCheckInCommand : IRequest<CdStaffAttendanceDto>
{
    public int UserId { get; set; }
    public int ProcessedByUserId { get; set; }
    public AttendanceMethod Method { get; set; } = AttendanceMethod.Manual;
    public int? CameraId { get; set; }
    public string? Notes { get; set; }
}
