using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Command to approve an invitation
/// </summary>
public class ApproveInvitationCommand : IRequest<InvitationDto>
{
    /// <summary>
    /// Invitation ID to approve
    /// </summary>
    [Required]
    public int InvitationId { get; set; }

    /// <summary>
    /// Approval comments
    /// </summary>
    [MaxLength(500)]
    public string? Comments { get; set; }

    /// <summary>
    /// User approving the invitation
    /// </summary>
    public int ApprovedBy { get; set; }
}
