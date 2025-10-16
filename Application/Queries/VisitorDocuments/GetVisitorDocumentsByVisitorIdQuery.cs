using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.VisitorDocuments;

/// <summary>
/// Query to get visitor documents by visitor ID
/// </summary>
public class GetVisitorDocumentsByVisitorIdQuery : IRequest<List<VisitorDocumentDto>>
{
    /// <summary>
    /// Visitor ID
    /// </summary>
    [Required]
    public int VisitorId { get; set; }

    /// <summary>
    /// Document type filter
    /// </summary>
    public string? DocumentType { get; set; }

    /// <summary>
    /// Include deleted documents
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}
