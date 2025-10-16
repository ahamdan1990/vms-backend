using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Command for deleting a visitor (soft delete)
/// </summary>
public class DeleteVisitorCommand : IRequest<bool>
{
    public int Id { get; set; }
    public int DeletedBy { get; set; }
    public bool PermanentDelete { get; set; } = false;
}
