using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Command for uploading user profile photo
/// </summary>
public class UploadProfilePhotoCommand : IRequest<string>
{
    public int UserId { get; set; }
    public IFormFile? File { get; set; }
}
