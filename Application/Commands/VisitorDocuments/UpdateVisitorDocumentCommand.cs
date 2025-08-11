using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorDocuments;

/// <summary>
/// Command to update a visitor document
/// </summary>
public class UpdateVisitorDocumentCommand : IRequest<VisitorDocumentDto>
{
    /// <summary>
    /// Document ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Document title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Document description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Document type
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Whether document contains sensitive information
    /// </summary>
    public bool IsSensitive { get; set; } = false;

    /// <summary>
    /// Whether document is required for check-in
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Document expiry date
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Additional tags
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// User updating the document
    /// </summary>
    public int ModifiedBy { get; set; }
}
