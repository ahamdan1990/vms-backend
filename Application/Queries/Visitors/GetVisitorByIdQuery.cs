using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Query for getting a visitor by ID
/// </summary>
public class GetVisitorByIdQuery : IRequest<VisitorDto?>
{
    public int Id { get; set; }
    public bool IncludeDeleted { get; set; } = false;
}
