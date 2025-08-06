using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Command for removing user profile photo
/// </summary>
public class RemoveProfilePhotoCommand : IRequest
{
    public int UserId { get; set; }
}
