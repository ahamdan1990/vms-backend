using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a document associated with a visitor
/// </summary>
public class VisitorDocument : SoftDeleteEntity
{
    /// <summary>
    /// Foreign key to the visitor
    /// </summary>
    [Required]
    public int VisitorId { get; set; }

    /// <summary>
    /// Document name/title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Document title (alias for DocumentName for consistency)
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
    /// Document type (ID, Photo, Contract, NDA, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Original file name
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;    /// <summary>
    /// Stored file name (typically GUID-based)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// File path where document is stored
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type of the document
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// MIME type alias for consistency
    /// </summary>
    public string MimeType
    {
        get => ContentType;
        set => ContentType = value;
    }

    /// <summary>
    /// File extension
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// File hash for integrity verification
    /// </summary>
    [MaxLength(100)]
    public string? FileHash { get; set; }

    /// <summary>
    /// Whether the document is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; } = false;    /// <summary>
    /// Whether the document contains sensitive information
    /// </summary>
    public bool IsSensitive { get; set; } = false;

    /// <summary>
    /// Whether the document is required for the visitor process
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Document expiration date (for temporary documents)
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
    /// Document version number
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Tags associated with the document
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Access level required to view the document
    /// </summary>
    [MaxLength(50)]
    public string AccessLevel { get; set; } = "Standard";

    /// <summary>
    /// Navigation property to the visitor
    /// </summary>
    public virtual Visitor Visitor { get; set; } = null!;    /// <summary>
    /// Checks if the document is expired
    /// </summary>
    /// <returns>True if the document is expired</returns>
    public bool IsExpired()
    {
        return ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the file size in a human-readable format
    /// </summary>
    /// <returns>Formatted file size</returns>
    public string GetFormattedFileSize()
    {
        if (FileSize == 0) return "0 B";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = FileSize;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Validates the document information
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateDocument()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(DocumentName))
            errors.Add("Document name is required.");

        if (string.IsNullOrWhiteSpace(DocumentType))
            errors.Add("Document type is required.");

        if (FileSize <= 0)
            errors.Add("File size must be greater than zero.");

        if (FileSize > 50 * 1024 * 1024) // 50MB limit
            errors.Add("File size cannot exceed 50MB.");

        return errors;
    }
}
