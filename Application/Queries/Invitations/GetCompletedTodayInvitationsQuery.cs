using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Returns invitations that both checked in and checked out during the current system day.
/// </summary>
public class GetCompletedTodayInvitationsQuery : IRequest<List<InvitationDto>>
{
    public int? LocationId { get; set; }
}
