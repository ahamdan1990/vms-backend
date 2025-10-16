using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Command for blacklisting a visitor
/// </summary>
public class BlacklistVisitorCommand : IRequest<bool>
{
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int BlacklistedBy { get; set; }
}
