using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Query to get invitation by invitation number
/// </summary>
public class GetInvitationByNumberQuery : IRequest<InvitationDto?>
{
    /// <summary>
    /// Invitation number to search for
    /// </summary>
    [Required]
    public string InvitationNumber { get; set; } = string.Empty;
}
