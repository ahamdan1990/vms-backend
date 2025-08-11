namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// Visitor document data transfer object
/// </summary>
public class VisitorDocumentDto
{
    /// <summary>
    /// Document ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Visitor ID
    /// </summary>
    public int VisitorId { get; set; }

    /// <summary>
    /// Document name
    /// </summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Document description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Document type
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Original file name
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// File size formatted for display
    /// </summary>
    public string FormattedFileSize { get; set; } = string.Empty;

    /// <summary>
    /// MIME type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File extension
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// Whether document is encrypted
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Whether document is sensitive
    /// </summary>
    public bool IsSensitive { get; set; }

    /// <summary>
    /// Document expiration date
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Whether document is expired
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Document version
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Access level
    /// </summary>
    public string AccessLevel { get; set; } = "Standard";

    /// <summary>
    /// Download URL
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Created by user name
    /// </summary>
    public string? CreatedByName { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Whether document is active
    /// </summary>
    public bool IsActive { get; set; }
    public object ModifiedByName { get; internal set; }
}
