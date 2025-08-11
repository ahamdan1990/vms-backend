using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.VisitorDocuments;

/// <summary>
/// Query to get a visitor document by ID
/// </summary>
public class GetVisitorDocumentByIdQuery : IRequest<VisitorDocumentDto?>
{
    /// <summary>
    /// Document ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Include deleted document
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}
