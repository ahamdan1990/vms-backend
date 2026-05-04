using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class CdStaffCheckOutCommand : IRequest<CdStaffAttendanceDto>
{
    public int UserId { get; set; }
    public int ProcessedByUserId { get; set; }
    public int? CameraId { get; set; }
}
