using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Command for an admin to assign a submitted invitation to UnderReview status
/// </summary>
public class AssignInvitationToReviewCommand : IRequest<InvitationDto>
{
    /// <summary>
    /// Invitation ID to assign for review
    /// </summary>
    [Required]
    public int InvitationId { get; set; }

    /// <summary>
    /// Admin user assigning the invitation for review
    /// </summary>
    public int AssignedBy { get; set; }
}
