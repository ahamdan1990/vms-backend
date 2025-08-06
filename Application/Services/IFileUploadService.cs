namespace VisitorManagementSystem.Api.Application.Services;

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
    string GetProfilePhotoUrl(string filePath);
}
