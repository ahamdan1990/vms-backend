using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorDocuments;

/// <summary>
/// Command to create a new visitor document
/// </summary>
public class CreateVisitorDocumentCommand : IRequest<VisitorDocumentDto>
{
    /// <summary>
    /// Visitor ID this document belongs to
    /// </summary>
    [Required]
    public int VisitorId { get; set; }

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
    /// File path or URL
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Original file name
    /// </summary>
    [MaxLength(255)]
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type
    /// </summary>
    [MaxLength(100)]
    public string? MimeType { get; set; }

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
    /// User uploading the document
    /// </summary>
    public int CreatedBy { get; set; }
}
