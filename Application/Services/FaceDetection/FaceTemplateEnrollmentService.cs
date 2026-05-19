using VisitorManagementSystem.Api.Application.DTOs.FaceDetection;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

public class FaceTemplateEnrollmentService : IFaceTemplateEnrollmentService
{
    private const string LuxandEngine = "Luxand";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;
    private readonly IFaceDetectionService _faceDetectionService;
    private readonly ILogger<FaceTemplateEnrollmentService> _logger;

    public FaceTemplateEnrollmentService(
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment,
        IFaceDetectionService faceDetectionService,
        ILogger<FaceTemplateEnrollmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _environment = environment;
        _faceDetectionService = faceDetectionService;
        _logger = logger;
    }

    public async Task<FaceTemplateEnrollmentBatchResultDto> EnrollExistingProfilePhotosAsync(
        bool includeVisitors,
        bool includeUsers,
        bool force,
        CancellationToken cancellationToken = default)
    {
        var result = new FaceTemplateEnrollmentBatchResultDto();

        if (!await _faceDetectionService.IsServiceAvailableAsync())
        {
            result.Items.Add(new FaceTemplateEnrollmentItemResultDto
            {
                PersonType = "System",
                Message = "No face recognition engine is available for enrollment.",
                Skipped = true
            });
            return result;
        }

        if (includeVisitors)
        {
            var visitors = await _unitOfWork.Visitors.GetAsync(
                visitor => visitor.ProfilePhotoPath != null && visitor.ProfilePhotoPath != string.Empty,
                cancellationToken);

            result.VisitorsScanned = visitors.Count;
            foreach (var visitor in visitors)
            {
                var item = await EnrollIdentityAsync(
                    "Visitor",
                    visitor.Id,
                    $"visitor:{visitor.Id}",
                    visitor.ProfilePhotoPath,
                    force,
                    cancellationToken);

                AddItem(result, item);
            }
        }

        if (includeUsers)
        {
            var users = await _unitOfWork.Users.GetAsync(
                user => user.ProfilePhotoPath != null && user.ProfilePhotoPath != string.Empty,
                cancellationToken);

            result.UsersScanned = users.Count;
            foreach (var user in users)
            {
                var item = await EnrollIdentityAsync(
                    "Staff",
                    user.Id,
                    $"user:{user.Id}",
                    user.ProfilePhotoPath,
                    force,
                    cancellationToken);

                AddItem(result, item);
            }
        }

        _logger.LogInformation(
            "Face template enrollment completed. VisitorsScanned={VisitorsScanned}, UsersScanned={UsersScanned}, Enrolled={Enrolled}, SkippedExisting={SkippedExisting}, SkippedMissingPhoto={SkippedMissingPhoto}, Failed={Failed}",
            result.VisitorsScanned,
            result.UsersScanned,
            result.Enrolled,
            result.SkippedExisting,
            result.SkippedMissingPhoto,
            result.Failed);

        return result;
    }

    private async Task<FaceTemplateEnrollmentItemResultDto> EnrollIdentityAsync(
        string personType,
        int personId,
        string subjectId,
        string? profilePhotoPath,
        bool force,
        CancellationToken cancellationToken)
    {
        var item = new FaceTemplateEnrollmentItemResultDto
        {
            PersonType = personType,
            PersonId = personId,
            SubjectId = subjectId,
            PhotoPath = profilePhotoPath
        };

        try
        {
            if (!force && await HasActiveLuxandTemplateAsync(personType, personId, cancellationToken))
            {
                item.Skipped = true;
                item.Message = "Active Luxand template already exists.";
                return item;
            }

            var localPath = ResolveLocalPhotoPath(profilePhotoPath);
            if (localPath == null || !File.Exists(localPath))
            {
                item.Skipped = true;
                item.Message = "Profile photo file was not found on disk.";
                return item;
            }

            var imageBytes = await File.ReadAllBytesAsync(localPath, cancellationToken);
            var enrollment = await _faceDetectionService.AddFaceToCollectionAsync(
                imageBytes,
                subjectId,
                cancellationToken);

            if (!enrollment.Success)
            {
                item.Success = false;
                item.Message = enrollment.ErrorMessage ?? "Face enrollment failed.";
                return item;
            }

            await _faceDetectionService.TrimFacesToMaxAsync(subjectId, 6, cancellationToken);

            item.Success = true;
            item.ImageId = enrollment.ImageId;
            item.Message = "Face template enrolled.";
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to enroll face template for {PersonType} {PersonId}",
                personType,
                personId);

            item.Success = false;
            item.Message = ex.Message;
            return item;
        }
    }

    private async Task<bool> HasActiveLuxandTemplateAsync(
        string personType,
        int personId,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<FaceTemplate>().AnyAsync(
            template => template.PersonType == personType &&
                        template.PersonId == personId &&
                        template.Engine == LuxandEngine &&
                        template.IsActive,
            cancellationToken);
    }

    private string? ResolveLocalPhotoPath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        if (Path.IsPathRooted(storedPath) && File.Exists(storedPath))
        {
            return storedPath;
        }

        if (Uri.TryCreate(storedPath, UriKind.Absolute, out _))
        {
            return null;
        }

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var relativePath = storedPath
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        var root = Path.GetFullPath(webRoot);
        if (!root.EndsWith(Path.DirectorySeparatorChar))
        {
            root += Path.DirectorySeparatorChar;
        }

        var candidate = Path.GetFullPath(Path.Combine(root, relativePath));

        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? candidate
            : null;
    }

    public async Task<FaceTemplateEnrollmentItemResultDto> EnrollVisitorPhotoAsync(
        int visitorId,
        byte[] imageBytes,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId, cancellationToken);
        if (visitor == null)
            return new FaceTemplateEnrollmentItemResultDto
            {
                PersonType = "Visitor", PersonId = visitorId,
                Success = false, Message = $"Visitor {visitorId} not found."
            };

        // Save image to disk
        var webRoot = _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadDir = Path.Combine(webRoot, "uploads", "visitors", visitorId.ToString());
        Directory.CreateDirectory(uploadDir);

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            ext = ".jpg";

        var fileName = $"visitor_{visitorId}_{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, fileName);
        var relativePath = $"uploads/visitors/{visitorId}/{fileName}";

        await File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);

        // Update Visitor.ProfilePhotoPath
        visitor.ProfilePhotoPath = relativePath;
        visitor.UpdateModifiedOn();
        _unitOfWork.Visitors.Update(visitor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Enroll face
        return await EnrollIdentityAsync(
            "Visitor",
            visitorId,
            $"visitor:{visitorId}",
            relativePath,
            force: true,
            cancellationToken);
    }

    private static void AddItem(
        FaceTemplateEnrollmentBatchResultDto result,
        FaceTemplateEnrollmentItemResultDto item)
    {
        result.Items.Add(item);

        if (item.Success)
        {
            result.Enrolled++;
            return;
        }

        if (item.Skipped && item.Message == "Active Luxand template already exists.")
        {
            result.SkippedExisting++;
            return;
        }

        if (item.Skipped && item.Message == "Profile photo file was not found on disk.")
        {
            result.SkippedMissingPhoto++;
            return;
        }

        result.Failed++;
    }
}
