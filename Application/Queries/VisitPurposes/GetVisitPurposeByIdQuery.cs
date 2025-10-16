using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;

namespace VisitorManagementSystem.Api.Application.Queries.VisitPurposes;

/// <summary>
/// Query to get visit purpose by ID
/// </summary>
public class GetVisitPurposeByIdQuery : IRequest<VisitPurposeDto?>
{
    /// <summary>
    /// Visit purpose ID
    /// </summary>
    [Required]
    public int Id { get; set; }
}
