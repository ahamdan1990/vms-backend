using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Command for removing blacklist status from a visitor
/// </summary>
public class RemoveBlacklistCommand : IRequest<bool>
{
    public int Id { get; set; }
    public int ModifiedBy { get; set; }
}
