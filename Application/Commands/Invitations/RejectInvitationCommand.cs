using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Command to reject an invitation
/// </summary>
public class RejectInvitationCommand : IRequest<InvitationDto>
{
    /// <summary>
    /// Invitation ID to reject
    /// </summary>
    [Required]
    public int InvitationId { get; set; }

    /// <summary>
    /// Rejection reason
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// User rejecting the invitation
    /// </summary>
    public int RejectedBy { get; set; }
}
