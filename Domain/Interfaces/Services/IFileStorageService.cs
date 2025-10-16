namespace VisitorManagementSystem.Api.Domain.Interfaces.Services;

/// <summary>
/// Interface for file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file to storage
    /// </summary>
    /// <param name="fileStream">File stream</param>
    /// <param name="fileName">File name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File path or URL</returns>
    Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a file to a specific folder
    /// </summary>
    /// <param name="fileStream">File stream</param>
    /// <param name="fileName">File name</param>
    /// <param name="folderPath">Folder path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File path or URL</returns>
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if file exists</returns>
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a file stream for reading
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream</returns>
    Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the public URL for a file
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <returns>Public URL</returns>
    string GetPublicUrl(string filePath);

    /// <summary>
    /// Gets file metadata
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata</returns>
    Task<FileMetadata> GetFileMetadataAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// File metadata information
/// </summary>
public class FileMetadata
{
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// File extension
    /// </summary>
    public string Extension { get; set; } = string.Empty;
}
