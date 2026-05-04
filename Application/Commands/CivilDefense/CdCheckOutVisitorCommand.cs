using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class CdCheckOutVisitorCommand : IRequest<InvitationDto>
{
    public int InvitationId { get; set; }
    public int OperatorUserId { get; set; }
}
