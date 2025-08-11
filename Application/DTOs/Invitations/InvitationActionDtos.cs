using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Invitations;

/// <summary>
/// DTO for approving an invitation
/// </summary>
public class ApproveInvitationDto
{
    /// <summary>
    /// Approval comments
    /// </summary>
    [MaxLength(500)]
    public string? Comments { get; set; }
}

/// <summary>
/// DTO for rejecting an invitation
/// </summary>
public class RejectInvitationDto
{
    /// <summary>
    /// Rejection reason
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

