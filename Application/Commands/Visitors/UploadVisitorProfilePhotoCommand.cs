using MediatR;
using Microsoft.AspNetCore.Http;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Command for uploading visitor profile photo
/// </summary>
public class UploadVisitorProfilePhotoCommand : IRequest<PhotoUploadResult>
{
    public int VisitorId { get; set; }
    public IFormFile? File { get; set; }
}
