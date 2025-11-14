using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Command for removing a visitor's profile photo
/// </summary>
public class RemoveVisitorProfilePhotoCommand : IRequest
{
    public int VisitorId { get; set; }
    public int ModifiedBy { get; set; }
}
