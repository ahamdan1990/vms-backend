using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Query for searching a visitor by face recognition from photo
/// </summary>
public class SearchVisitorByPhotoQuery : IRequest<VisitorDto?>
{
    public IFormFile Photo { get; set; } = null!;
}
