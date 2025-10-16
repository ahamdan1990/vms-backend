using MediatR;
using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.Commands.Locations;

/// <summary>
/// Command to delete a location
/// </summary>
public class DeleteLocationCommand : IRequest<bool>
{
    /// <summary>
    /// Location ID to delete
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// User deleting the location
    /// </summary>
    public int DeletedBy { get; set; }

    /// <summary>
    /// Whether to perform a soft delete (default) or hard delete
    /// </summary>
    public bool SoftDelete { get; set; } = true;
}
