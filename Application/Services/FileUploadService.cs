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

    public async Task<string> UploadVisitorDocumentAsync(int visitorId, IFormFile file, string documentType)
    {
        try
        {
            // Validate file
            if (!IsValidDocumentFile(file))
            {
                throw new ArgumentException("Invalid document file type or size");
            }

            // Verify visitor exists
            var visitor = await _unitOfWork.Repository<Domain.Entities.Visitor>().GetByIdAsync(visitorId);
            if (visitor == null)
            {
                throw new InvalidOperationException($"Visitor with ID {visitorId} not found");
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"visitor_{visitorId}_{documentType}_{Guid.NewGuid()}{fileExtension}";
            
            // Create upload directory if it doesn't exist
            var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "visitor-documents", visitorId.ToString());
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, fileName);
            var relativePath = $"uploads/visitor-documents/{visitorId}/{fileName}";

            // Save file
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            _logger.LogInformation("Successfully uploaded visitor document for visitor {VisitorId}: {FileName}", 
                visitorId, fileName);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload visitor document for visitor {VisitorId}", visitorId);
            throw;
        }
    }

    public async Task RemoveVisitorDocumentAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Successfully removed visitor document: {FilePath}", filePath);
            }
            else
            {
                _logger.LogWarning("Visitor document file not found: {FilePath}", filePath);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove visitor document: {FilePath}", filePath);
            throw;
        }
    }
    public bool IsValidDocumentFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return false;

        // Check file size (max 10MB)
        var maxSize = GetMaxDocumentFileSize();
        if (file.Length > maxSize)
            return false;

        // Check file extension
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = GetAllowedDocumentExtensions();
        
        if (!allowedExtensions.Contains(fileExtension))
            return false;

        // Check MIME type
        var allowedMimeTypes = new[]
        {
            "application/pdf",
            "image/jpeg",
            "image/jpg", 
            "image/png",
            "image/gif",
            "image/bmp",
            "image/tiff",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return false;

        return true;
    }

    public string GetVisitorDocumentUrl(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;

        var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5000";
        return $"{baseUrl.TrimEnd('/')}/{filePath.TrimStart('/')}";
    }

    public List<string> GetAllowedDocumentExtensions()
    {
        return new List<string>
        {
            ".pdf",    // PDF documents
            ".jpg",    // JPEG images
            ".jpeg",   // JPEG images
            ".png",    // PNG images
            ".gif",    // GIF images
            ".bmp",    // Bitmap images
            ".tiff",   // TIFF images
            ".doc",    // Word documents
            ".docx"    // Word documents (newer format)
        };
    }

    public long GetMaxDocumentFileSize()
    {
        // Default to 10MB, but allow configuration override
        var configSize = _configuration.GetValue<long?>("FileUpload:MaxDocumentSize");
        return configSize ?? (10 * 1024 * 1024); // 10MB in bytes
    }
}
