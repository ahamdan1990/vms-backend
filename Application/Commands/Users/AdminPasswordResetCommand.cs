// Update your AdminPasswordResetCommand to include missing properties
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Controllers;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Command for admin password reset
/// </summary>
public class AdminPasswordResetCommand : IRequest<CommandResultDto<AdminPasswordResetResponseDto>>
{
    public int UserId { get; set; }
    public string? NewPassword { get; set; }
    public bool GenerateTemporaryPassword { get; set; } = true;
    public bool RequirePasswordChange { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public bool NotifyUser { get; set; } = true;
    public int ResetBy { get; set; }
    public string? Reason { get; set; }
}