using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Command to cancel an invitation
/// </summary>
public class CancelInvitationCommand : IRequest<InvitationDto>
{
    /// <summary>
    /// Invitation ID to cancel
    /// </summary>
    [Required]
    public int InvitationId { get; set; }

    /// <summary>
    /// Cancellation reason
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// User cancelling the invitation
    /// </summary>
    public int CancelledBy { get; set; }
}
