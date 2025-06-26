using MediatR;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Application.Commands.Auth;

/// <summary>
/// Command for changing user password
/// </summary>
public class ChangePasswordCommand : IRequest<PasswordChangeResult>
{
    public int UserId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool InvalidateAllSessions { get; set; } = true;
}

