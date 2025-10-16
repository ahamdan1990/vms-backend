using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.VisitorNotes;

/// <summary>
/// Query to get visitor notes by visitor ID
/// </summary>
public class GetVisitorNotesByVisitorIdQuery : IRequest<List<VisitorNoteDto>>
{
    /// <summary>
    /// Visitor ID
    /// </summary>
    [Required]
    public int VisitorId { get; set; }

    /// <summary>
    /// Category filter
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Only flagged notes
    /// </summary>
    public bool? IsFlagged { get; set; }

    /// <summary>
    /// Only confidential notes
    /// </summary>
    public bool? IsConfidential { get; set; }

    /// <summary>
    /// Include deleted notes
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}
