using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class CdRegisterAndCheckInResultDto
{
    public VisitorDto Visitor { get; set; } = null!;
    public InvitationDto Invitation { get; set; } = null!;
    public bool WasExistingVisitor { get; set; }
}
