using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Command for removing VIP status from a visitor
/// </summary>
public class RemoveVipStatusCommand : IRequest<bool>
{
    public int Id { get; set; }
    public int ModifiedBy { get; set; }
}
