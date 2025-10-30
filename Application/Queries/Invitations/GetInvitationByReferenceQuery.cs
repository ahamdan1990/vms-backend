using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Query to get an invitation by reference (ID, invitation number, or QR code)
/// </summary>
public class GetInvitationByReferenceQuery : IRequest<InvitationDto?>
{
    /// <summary>
    /// Invitation reference (ID, invitation number, or QR code data)
    /// </summary>
    [Required]
    public string Reference { get; set; } = string.Empty;

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
