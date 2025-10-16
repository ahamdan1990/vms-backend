using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Query to get an invitation by ID
/// </summary>
public class GetInvitationByIdQuery : IRequest<InvitationDto?>
{
    /// <summary>
    /// Invitation ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Include deleted invitation
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>
    /// Include events timeline
    /// </summary>
    public bool IncludeEvents { get; set; } = false;

    /// <summary>
    /// Include approvals workflow
    /// </summary>
    public bool IncludeApprovals { get; set; } = false;
}
