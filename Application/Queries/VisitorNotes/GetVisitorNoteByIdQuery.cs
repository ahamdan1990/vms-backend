using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.VisitorNotes;

/// <summary>
/// Query to get a visitor note by ID
/// </summary>
public class GetVisitorNoteByIdQuery : IRequest<VisitorNoteDto?>
{
    /// <summary>
    /// Note ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Include deleted note
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}
