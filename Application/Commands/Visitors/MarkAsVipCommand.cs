using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Command for marking a visitor as VIP
/// </summary>
public class MarkAsVipCommand : IRequest<bool>
{
    public int Id { get; set; }
    public int ModifiedBy { get; set; }
}
