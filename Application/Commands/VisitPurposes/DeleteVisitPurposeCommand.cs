using MediatR;
using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.Commands.VisitPurposes;

/// <summary>
/// Command to delete a visit purpose
/// </summary>
public class DeleteVisitPurposeCommand : IRequest<bool>
{
    /// <summary>
    /// Visit purpose ID to delete
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// User deleting the visit purpose
    /// </summary>
    public int DeletedBy { get; set; }

    /// <summary>
    /// Whether to perform a soft delete (default) or hard delete
    /// </summary>
    public bool SoftDelete { get; set; } = true;
}
