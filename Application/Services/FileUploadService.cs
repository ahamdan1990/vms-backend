using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace VisitorManagementSystem.Api.Application.Services;

/// <summary>
/// Implementation of file upload service for handling profile photos
/// </summary>
public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FileUploadService> _logger;
    private readonly IConfiguration _configuration;

    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
    private readonly int _maxWidth = 800;
    private readonly int _maxHeight = 800;

    public FileUploadService(
        IWebHostEnvironment environment,
        IUnitOfWork unitOfWork,
        ILogger<FileUploadService> logger,
        IConfiguration configuration)
    {
        _environment = environment;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> UploadProfilePhotoAsync(int userId, IFormFile file)
    {
        try
        {
            // Validate file
            if (!IsValidImageFile(file))
            {
                throw new ArgumentException("Invalid image file");
            }

            // Get user
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            // Remove existing photo if exists
            if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
            {
                await RemoveExistingPhoto(user.ProfilePhotoPath);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"profile_{userId}_{Guid.NewGuid()}{fileExtension}";
            
            // Create upload directory if it doesn't exist
            var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, fileName);
            var relativePath = $"uploads/profiles/{fileName}";

            // Process and save image
            using var image = await Image.LoadAsync(file.OpenReadStream());
            
            // Resize if too large
            if (image.Width > _maxWidth || image.Height > _maxHeight)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(_maxWidth, _maxHeight),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                }));
            }

            await image.SaveAsync(filePath);

            // Update user profile photo path
            user.ProfilePhotoPath = relativePath;
            user.UpdateModifiedOn();
            
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Profile photo uploaded successfully for user {UserId}: {FilePath}", 
                userId, relativePath);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile photo for user {UserId}", userId);
            throw;
        }
    }
    public async Task RemoveProfilePhotoAsync(int userId)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
            {
                await RemoveExistingPhoto(user.ProfilePhotoPath);

                // Clear photo path from user
                user.ProfilePhotoPath = null;
                user.UpdateModifiedOn();
                
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Profile photo removed for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing profile photo for user {UserId}", userId);
            throw;
        }
    }

    public bool IsValidImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return false;
        }

        // Check file size
        if (file.Length > _maxFileSize)
        {
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return false;
        }

        // Check content type
        var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
        if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return false;
        }

        return true;
    }

    public string GetProfilePhotoUrl(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5000";
        return $"{baseUrl.TrimEnd('/')}/{filePath.Replace('\\', '/')}";
    }

    private async Task RemoveExistingPhoto(string relativePath)
    {
        try
        {
            var fullPath = Path.Combine(_environment.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogDebug("Deleted existing photo: {FilePath}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete existing photo: {FilePath}", relativePath);
            // Don't throw - continue with upload even if old file can't be deleted
        }
    }
}
