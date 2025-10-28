using MediatR;
using Microsoft.AspNetCore.Http;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Command for uploading visitor profile photo
/// </summary>
public class UploadVisitorProfilePhotoCommand : IRequest<string>
{
    public int VisitorId { get; set; }
    public IFormFile? File { get; set; }
}
