using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Command to submit an invitation for approval
/// </summary>
public class SubmitInvitationCommand : IRequest<InvitationDto>
{
    /// <summary>
    /// Invitation ID to submit
    /// </summary>
    [Required]
    public int InvitationId { get; set; }

    /// <summary>
    /// User submitting the invitation
    /// </summary>
    public int SubmittedBy { get; set; }
}

/// <summary>
/// Command to check in a visitor using invitation
/// </summary>
public class CheckInInvitationCommand : IRequest<InvitationDto>
{
    /// <summary>
    /// Invitation ID or QR code data
    /// </summary>
    [Required]
    public string InvitationReference { get; set; } = string.Empty;

    /// <summary>
    /// User processing the check-in
    /// </summary>
    public int CheckedInBy { get; set; }

    /// <summary>
    /// Additional check-in notes
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Command to check out a visitor
/// </summary>
public class CheckOutInvitationCommand : IRequest<InvitationDto>
{
    /// <summary>
    /// Invitation ID
    /// </summary>
    [Required]
    public int InvitationId { get; set; }

    /// <summary>
    /// User processing the check-out
    /// </summary>
    public int CheckedOutBy { get; set; }

    /// <summary>
    /// Additional check-out notes
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
