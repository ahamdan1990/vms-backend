using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Command for updating current user's preferences
/// </summary>
public class UpdateUserPreferencesCommand : IRequest<UserProfileDto>
{
    public int UserId { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public string Language { get; set; } = "en-US";
    public string Theme { get; set; } = "light";
}
