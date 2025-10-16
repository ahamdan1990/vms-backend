using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// DTO for updating an existing visitor document
/// </summary>
public class UpdateVisitorDocumentDto
{
    /// <summary>
    /// Document name/title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Title alias for consistency
    /// </summary>
    public string Title
    {
        get => DocumentName;
        set => DocumentName = value;
    }

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
    /// Whether document is sensitive
    /// </summary>
    public bool IsSensitive { get; set; } = false;

    /// <summary>
    /// Whether the document is required
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Document expiration date
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Expiry date alias for consistency
    /// </summary>
    public DateTime? ExpiryDate
    {
        get => ExpirationDate;
        set => ExpirationDate = value;
    }

    /// <summary>
    /// Tags associated with document
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Access level required
    /// </summary>
    [MaxLength(50)]
    public string AccessLevel { get; set; } = "Standard";
}
