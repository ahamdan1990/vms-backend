using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Command for unlocking a user account
/// </summary>
public class UnlockUserCommand : IRequest<CommandResultDto<object>>
{
    public int UserId { get; set; }
    public int UnlockedBy { get; set; }
    public string? Reason { get; set; }
    public bool ResetFailedAttempts { get; set; } = true;
}
