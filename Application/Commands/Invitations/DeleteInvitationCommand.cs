using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Command for deleting an invitation (only if status is Cancelled)
/// </summary>
public class DeleteInvitationCommand : IRequest<bool>
{
    public int Id { get; set; }
    public int DeletedBy { get; set; }
}
