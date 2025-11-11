namespace VisitorManagementSystem.Api.Application.Services.FileUploadService;

/// <summary>
/// Service for handling file uploads, particularly profile photos
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// Uploads a profile photo for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="file">Image file</param>
    /// <returns>File path/URL of uploaded photo</returns>
    Task<string> UploadProfilePhotoAsync(int userId, IFormFile file);

    /// <summary>
    /// Removes a user's profile photo
    /// </summary>
    /// <param name="userId">User ID</param>
    Task RemoveProfilePhotoAsync(int userId);

    /// <summary>
    /// Validates if file is a valid image
    /// </summary>
    /// <param name="file">File to validate</param>
    /// <returns>True if valid image</returns>
    bool IsValidImageFile(IFormFile file);

    /// <summary>
    /// Gets the full URL for a profile photo
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <returns>Full URL</returns>
    string? GetProfilePhotoUrl(string? filePath);

    /// <summary>
    /// Uploads a visitor document
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="file">Document file</param>
    /// <param name="documentType">Type of document</param>
    /// <returns>File path/URL of uploaded document</returns>
    Task<string> UploadVisitorDocumentAsync(int visitorId, IFormFile file, string documentType);

    /// <summary>
    /// Removes a visitor document file
    /// </summary>
    /// <param name="filePath">File path to remove</param>
    Task RemoveVisitorDocumentAsync(string filePath);

    /// <summary>
    /// Validates if file is a valid visitor document
    /// </summary>
    /// <param name="file">File to validate</param>
    /// <returns>True if valid document</returns>
    bool IsValidDocumentFile(IFormFile file);

    /// <summary>
    /// Gets the full URL for a visitor document
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <returns>Full URL</returns>
    string GetVisitorDocumentUrl(string filePath);

    /// <summary>
    /// Gets allowed document file extensions
    /// </summary>
    /// <returns>List of allowed extensions</returns>
    List<string> GetAllowedDocumentExtensions();

    /// <summary>
    /// Gets maximum document file size in bytes
    /// </summary>
    /// <returns>Maximum file size</returns>
    long GetMaxDocumentFileSize();
}

/// <summary>
/// Document upload result
/// </summary>
public class DocumentUploadResult
{
    /// <summary>
    /// Whether the upload was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// File path if successful
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Original file name
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static DocumentUploadResult CreateSuccess(string filePath, string originalFileName, long fileSize, string mimeType)
    {
        return new DocumentUploadResult
        {
            Success = true,
            FilePath = filePath,
            OriginalFileName = originalFileName,
            FileSize = fileSize,
            MimeType = mimeType
        };
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static DocumentUploadResult Failure(string errorMessage)
    {
        return new DocumentUploadResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
