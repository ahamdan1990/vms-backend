using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class CdCheckInVisitorCommand : IRequest<InvitationDto>
{
    public int InvitationId { get; set; }
    public int OperatorUserId { get; set; }
    public int? CameraId { get; set; }
    public bool Force { get; set; } = false;
}
