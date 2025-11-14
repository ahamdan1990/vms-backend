using AutoMapper;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Visitors;

/// <summary>
/// Service for visitor business logic
/// </summary>
public class VisitorService : IVisitorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<VisitorService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IFaceDetectionService _faceDetectionService;
    private readonly CompreFaceSettings _compreFaceSettings;

    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };
    private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
    private readonly int _maxWidth = 800;
    private readonly int _maxHeight = 800;

    public VisitorService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<VisitorService> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IFaceDetectionService faceDetectionService,
        IOptions<CompreFaceSettings> compreFaceSettings)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
        _faceDetectionService = faceDetectionService;
        _compreFaceSettings = compreFaceSettings.Value;
    }

    public async Task<ValidationResult> ValidateVisitorForCreationAsync(CreateVisitorDto createDto)
    {
        try
        {
            var errors = new List<string>();

            // Check email uniqueness
            if (await _unitOfWork.Visitors.EmailExistsAsync(createDto.Email))
            {
                errors.Add($"A visitor with email '{createDto.Email}' already exists.");
            }

            // Check government ID uniqueness if provided
            if (!string.IsNullOrEmpty(createDto.GovernmentId) &&
                await _unitOfWork.Visitors.GovernmentIdExistsAsync(createDto.GovernmentId))
            {
                errors.Add($"A visitor with government ID '{createDto.GovernmentId}' already exists.");
            }

            // Validate age if date of birth is provided
            if (createDto.DateOfBirth.HasValue)
            {
                var age = DateTime.Today.Year - createDto.DateOfBirth.Value.Year;
                if (createDto.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age))
                    age--;

                if (age < 16)
                {
                    errors.Add("Visitor must be at least 16 years old.");
                }

                if (age > 120)
                {
                    errors.Add("Invalid date of birth.");
                }
            }

            // Validate emergency contacts
            var emergencyContactErrors = ValidateEmergencyContacts(createDto.EmergencyContacts);
            errors.AddRange(emergencyContactErrors);

            return errors.Any() ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating visitor for creation");
            return ValidationResult.Failure("Validation failed due to system error.");
        }
    }

    public async Task<ValidationResult> ValidateVisitorForUpdateAsync(int id, UpdateVisitorDto updateDto)
    {
        try
        {
            var errors = new List<string>();

            // Check if visitor exists
            var existingVisitor = await _unitOfWork.Visitors.GetByIdAsync(id);
            if (existingVisitor == null)
            {
                errors.Add($"Visitor with ID '{id}' not found.");
                return ValidationResult.Failure(errors.ToArray());
            }

            // Check if visitor is deleted
            if (existingVisitor.IsDeleted)
            {
                errors.Add("Cannot update deleted visitor.");
            }

            // Check email uniqueness (excluding current visitor)
            if (await _unitOfWork.Visitors.EmailExistsAsync(updateDto.Email, id))
            {
                errors.Add($"A visitor with email '{updateDto.Email}' already exists.");
            }

            // Check government ID uniqueness if provided (excluding current visitor)
            if (!string.IsNullOrEmpty(updateDto.GovernmentId) &&
                await _unitOfWork.Visitors.GovernmentIdExistsAsync(updateDto.GovernmentId, id))
            {
                errors.Add($"A visitor with government ID '{updateDto.GovernmentId}' already exists.");
            }

            // Validate age if date of birth is provided
            if (updateDto.DateOfBirth.HasValue)
            {
                var age = DateTime.Today.Year - updateDto.DateOfBirth.Value.Year;
                if (updateDto.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age))
                    age--;

                if (age < 16)
                {
                    errors.Add("Visitor must be at least 16 years old.");
                }

                if (age > 120)
                {
                    errors.Add("Invalid date of birth.");
                }
            }

            return errors.Any() ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating visitor for update: {Id}", id);
            return ValidationResult.Failure("Validation failed due to system error.");
        }
    }

    public async Task<ValidationResult> ValidateVisitorForDeletionAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var errors = new List<string>();

            // Check if visitor exists
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(id);
            if (visitor == null)
            {
                errors.Add($"Visitor with ID '{id}' not found.");
                return ValidationResult.Failure(errors.ToArray());
            }

            // Check if visitor is already deleted
            if (visitor.IsDeleted)
            {
                errors.Add("Visitor is already deleted.");
            }

            // Business rules for visitor deletion
            // Check if visitor has active invitations
            var hasActiveInvitations = await _unitOfWork.Repository<Domain.Entities.Invitation>()
                .AnyAsync(i => i.VisitorId == id && 
                              (i.Status == Domain.Enums.InvitationStatus.Approved || 
                               i.Status == Domain.Enums.InvitationStatus.Active), cancellationToken);

            if (hasActiveInvitations)
            {
                errors.Add("Cannot delete visitor with active invitations. Please cancel or complete all invitations first.");
            }

            // Check if visitor has recent visits (within last 30 days)
            var hasRecentVisits = await _unitOfWork.Repository<Domain.Entities.Invitation>()
                .AnyAsync(i => i.VisitorId == id && 
                              i.CheckedInAt.HasValue && 
                              i.CheckedInAt.Value >= DateTime.UtcNow.AddDays(-30), cancellationToken);

            if (hasRecentVisits)
            {
                errors.Add("Cannot delete visitor with recent visits (within 30 days). Consider archiving instead.");
            }

            return errors.Any() ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating visitor for deletion: {Id}", id);
            return ValidationResult.Failure("Validation failed due to system error.");
        }
    }

    public async Task<VisitorDto> MergeVisitorsAsync(int primaryVisitorId, List<int> duplicateVisitorIds, int mergedBy)
    {
        try
        {
            _logger.LogInformation("Starting visitor merge operation - Primary: {PrimaryId}, Duplicates: {DuplicateIds}",
                primaryVisitorId, string.Join(",", duplicateVisitorIds));

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            
            // Get primary visitor
            var primaryVisitor = await _unitOfWork.Visitors.GetByIdAsync(primaryVisitorId, 
                v => v.EmergencyContacts, v => v.Documents, v => v.VisitorNotes);
            
            if (primaryVisitor == null)
            {
                throw new InvalidOperationException($"Primary visitor with ID '{primaryVisitorId}' not found.");
            }

            // Get duplicate visitors
            var duplicateVisitors = new List<Visitor>();
            foreach (var duplicateId in duplicateVisitorIds)
            {
                var duplicate = await _unitOfWork.Visitors.GetByIdAsync(duplicateId,
                    v => v.EmergencyContacts, v => v.Documents, v => v.VisitorNotes);
                if (duplicate != null)
                {
                    duplicateVisitors.Add(duplicate);
                }
            }

            // Merge data from duplicates to primary
            foreach (var duplicate in duplicateVisitors)
            {
                // Merge emergency contacts
                foreach (var contact in duplicate.EmergencyContacts)
                {
                    contact.VisitorId = primaryVisitorId;
                    _unitOfWork.EmergencyContacts.Update(contact);
                }

                // Merge documents
                foreach (var document in duplicate.Documents)
                {
                    document.VisitorId = primaryVisitorId;
                    _unitOfWork.VisitorDocuments.Update(document);
                }

                // Merge notes
                foreach (var note in duplicate.VisitorNotes)
                {
                    note.VisitorId = primaryVisitorId;
                    _unitOfWork.VisitorNotes.Update(note);
                }

                // Update visit statistics
                primaryVisitor.VisitCount += duplicate.VisitCount;
                if (duplicate.LastVisitDate.HasValue && 
                    (primaryVisitor.LastVisitDate == null || duplicate.LastVisitDate > primaryVisitor.LastVisitDate))
                {
                    primaryVisitor.LastVisitDate = duplicate.LastVisitDate;
                }

                // Soft delete duplicate visitor
                duplicate.SoftDelete(mergedBy);
                _unitOfWork.Visitors.Update(duplicate);
            }

            // Update primary visitor
            primaryVisitor.UpdateModifiedBy(mergedBy);
            _unitOfWork.Visitors.Update(primaryVisitor);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Visitor merge completed successfully - Primary: {PrimaryId}", primaryVisitorId);

            // Return updated primary visitor
            var mergedVisitor = await GetCompleteVisitorProfileAsync(primaryVisitorId);
            return mergedVisitor!;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error merging visitors - Primary: {PrimaryId}", primaryVisitorId);
            throw;
        }
    }
    public async Task<List<VisitorDto>> FindPotentialDuplicatesAsync(Visitor visitor)
    {
        try
        {
            var duplicates = await _unitOfWork.Visitors.GetPotentialDuplicatesAsync();
            var potentialDuplicates = new List<VisitorDto>();

            foreach (var group in duplicates)
            {
                if (group.Any(v => v.Id == visitor.Id || 
                    v.Email.Value.Equals(visitor.Email.Value, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(v.GovernmentId) && !string.IsNullOrEmpty(visitor.GovernmentId) &&
                     v.GovernmentId.Equals(visitor.GovernmentId, StringComparison.OrdinalIgnoreCase))))
                {
                    var mappedGroup = _mapper.Map<List<VisitorDto>>(group.Where(v => v.Id != visitor.Id));
                    potentialDuplicates.AddRange(mappedGroup);
                }
            }

            return potentialDuplicates.DistinctBy(v => v.Id).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding potential duplicates for visitor: {VisitorId}", visitor.Id);
            throw;
        }
    }

    public async Task<PhotoUploadResult> UploadVisitorPhotoAsync(int visitorId, IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file provided");
            }

            // Check file size
            if (file.Length > _maxFileSize)
            {
                throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Invalid file type. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            // Get visitor
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId, cancellationToken);
            if (visitor == null)
            {
                throw new InvalidOperationException($"Visitor with ID {visitorId} not found");
            }

            // Create subject ID using visitor's email for CompreFace (guaranteed unique)
            // Fall back to name if email is not available
            string nameId = $"{visitor.FirstName}_{visitor.LastName}".Replace(" ", "_");
            var subjectId = !string.IsNullOrEmpty(visitor.Email)
                ? ((string)visitor.Email).ToLower(System.Globalization.CultureInfo.InvariantCulture)
                : nameId.ToLower(System.Globalization.CultureInfo.InvariantCulture);

            // Check if CompreFace is enabled and available
            bool requireFaceDetection = false;
            if (_compreFaceSettings.Enabled)
            {
                try
                {
                    var isAvailable = await _faceDetectionService.IsServiceAvailableAsync();
                    if (isAvailable)
                    {
                        requireFaceDetection = true;
                        _logger.LogDebug("CompreFace is enabled and available - face detection required");
                    }
                    else
                    {
                        _logger.LogWarning("CompreFace is enabled but not available - proceeding without face detection");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to check CompreFace availability - proceeding without face detection");
                }
            }

            // Remove existing photo if exists
            if (!string.IsNullOrEmpty(visitor.ProfilePhotoPath))
            {
                await RemoveExistingPhoto(visitor.ProfilePhotoPath);

                // Also remove from CompreFace face collection using visitor's name if available
                if (requireFaceDetection)
                {
                    await _faceDetectionService.RemoveFaceFromCollectionAsync(subjectId, cancellationToken);
                }
            }

            // Try to detect and crop face from the uploaded image
            byte[]? imageBytes = null;
            bool faceDetected = false;
            bool faceRecognitionEnabled = false;
            string? warningMessage = null;
            PhotoUploadWarningType? warningType = null;
            bool shouldRetry = false;

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                // Attempt face detection and cropping with 40% margin for better framing
                var croppedFaceBytes = await _faceDetectionService.DetectAndCropFaceAsync(
                    memoryStream,
                    marginPercent: 40,
                    cancellationToken);

                if (croppedFaceBytes != null && croppedFaceBytes.Length > 0)
                {
                    _logger.LogInformation("Face detected and cropped for visitor {VisitorId} ({VisitorName})",
                        visitorId, subjectId);
                    imageBytes = croppedFaceBytes;
                    faceDetected = true;
                }
                else
                {
                    // No face detected
                    if (requireFaceDetection)
                    {
                        // REJECT the upload when face detection is required but no face found
                        _logger.LogWarning("No face detected for visitor {VisitorId} ({VisitorName}), rejecting photo upload",
                            visitorId, subjectId);
                        throw new InvalidOperationException("No face detected in the uploaded image. Please upload a photo with a clearly visible face, or take a new photo if using a camera.");
                    }
                    else
                    {
                        // If face detection is not available/required, save the original image
                        _logger.LogInformation("Face detection service not available, saving original image for visitor {VisitorId}",
                            visitorId);
                        memoryStream.Position = 0;
                        imageBytes = memoryStream.ToArray();
                        faceDetected = false;
                    }
                }
            }

            // Generate unique filename
            var fileName = $"visitor_{visitorId}_{Guid.NewGuid()}.jpg"; // Always save as JPEG

            // Create upload directory if it doesn't exist
            var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "visitors", visitorId.ToString());
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, fileName);
            var relativePath = $"uploads/visitors/{visitorId}/{fileName}";

            // Process and save image
            using (var imageStream = new MemoryStream(imageBytes))
            {
                using var image = await Image.LoadAsync(imageStream, cancellationToken);

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

                await image.SaveAsync(filePath, cancellationToken);
            }

            // Update visitor profile photo path
            visitor.ProfilePhotoPath = relativePath;
            visitor.UpdateModifiedOn();

            _unitOfWork.Visitors.Update(visitor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Profile photo saved successfully for visitor {VisitorId}: {FilePath}",
                visitorId, relativePath);

            // Add face to CompreFace recognition collection for future recognition (only if face was detected)
            if (requireFaceDetection && faceDetected)
            {
                try
                {
                    var addFaceResult = await _faceDetectionService.AddFaceToCollectionAsync(
                        imageBytes,
                        subjectId,
                        cancellationToken);

                    if (addFaceResult.Success)
                    {
                        _logger.LogInformation("Face added to recognition collection for visitor {VisitorId} ({VisitorName}), image_id: {ImageId}",
                            visitorId, subjectId, addFaceResult.ImageId);
                        faceRecognitionEnabled = true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to add face to recognition collection for visitor {VisitorId} ({VisitorName}): {Error}",
                            visitorId, subjectId, addFaceResult.ErrorMessage);

                        // Determine if error is transient
                        bool isTransientError = addFaceResult.ErrorMessage?.Contains("500") == true ||
                                               addFaceResult.ErrorMessage?.Contains("timeout") == true ||
                                               addFaceResult.ErrorMessage?.Contains("synchronization") == true;

                        if (isTransientError)
                        {
                            warningMessage = "Photo uploaded successfully with face detected, but the recognition service encountered a temporary error. Please try uploading again or contact support if the issue persists.";
                            warningType = PhotoUploadWarningType.ServiceError;
                            shouldRetry = true;
                        }
                        else
                        {
                            warningMessage = "Photo uploaded successfully with face detected, but face could not be added to recognition system. Face recognition may not work for this visitor.";
                            warningType = PhotoUploadWarningType.PartialSuccess;
                            shouldRetry = false;
                        }
                        faceRecognitionEnabled = false;
                    }
                }
                catch (Exception ex)
                {
                    // Don't fail the entire operation if CompreFace is unavailable
                    _logger.LogError(ex, "Error adding face to CompreFace collection for visitor {VisitorId} ({VisitorName})",
                        visitorId, subjectId);

                    warningMessage = "Photo uploaded successfully with face detected, but face recognition service encountered an error. Face recognition may not work for this visitor. You may try uploading again.";
                    warningType = PhotoUploadWarningType.ServiceError;
                    shouldRetry = true;
                    faceRecognitionEnabled = false;
                }
            }
            else if (!faceDetected)
            {
                _logger.LogDebug("Skipping CompreFace face collection addition - no face detected");
                faceRecognitionEnabled = false;
            }
            else
            {
                _logger.LogDebug("Skipping CompreFace face collection addition - service not available");
                faceRecognitionEnabled = false;
            }

            // Return full URL with status
            var baseUrl = _configuration["BaseUrl"] ?? "https://192.168.0.24:7000";
            return new PhotoUploadResult
            {
                PhotoUrl = $"{baseUrl.TrimEnd('/')}/{relativePath.Replace('\\', '/')}",
                FaceDetected = faceDetected,
                FaceRecognitionEnabled = faceRecognitionEnabled,
                WarningMessage = warningMessage,
                WarningType = warningType,
                ShouldRetry = shouldRetry
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading profile photo for visitor {VisitorId}", visitorId);
            throw;
        }
    }

    public async Task<VisitorDto> UpdateProfilePhotoAsync(int visitorId, string photoPath, int updatedBy)
    {
        try
        {
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId);
            if (visitor == null)
            {
                throw new InvalidOperationException($"Visitor with ID '{visitorId}' not found.");
            }

            visitor.ProfilePhotoPath = photoPath;
            visitor.UpdateModifiedBy(updatedBy);

            _unitOfWork.Visitors.Update(visitor);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Profile photo updated for visitor: {VisitorId} by {UpdatedBy}",
                visitorId, updatedBy);

            return _mapper.Map<VisitorDto>(visitor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile photo for visitor: {VisitorId}", visitorId);
            throw;
        }
    }

    public async Task<VisitorDto> RemoveProfilePhotoAsync(int visitorId, int updatedBy)
    {
        try
        {
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId);
            if (visitor == null)
            {
                throw new InvalidOperationException($"Visitor with ID '{visitorId}' not found.");
            }

            visitor.ProfilePhotoPath = null;
            visitor.UpdateModifiedBy(updatedBy);

            _unitOfWork.Visitors.Update(visitor);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Profile photo removed for visitor: {VisitorId} by {UpdatedBy}", 
                visitorId, updatedBy);

            return _mapper.Map<VisitorDto>(visitor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing profile photo for visitor: {VisitorId}", visitorId);
            throw;
        }
    }

    public async Task<VisitorDto?> GetCompleteVisitorProfileAsync(int visitorId)
    {
        try
        {
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId,
                v => v.EmergencyContacts,
                v => v.Documents,
                v => v.VisitorNotes,
                v => v.CreatedByUser!,
                v => v.ModifiedByUser!,
                v => v.BlacklistedByUser!);

            return visitor == null ? null : _mapper.Map<VisitorDto>(visitor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting complete visitor profile: {VisitorId}", visitorId);
            throw;
        }
    }

    public async Task<int> ArchiveOldVisitorsAsync(int olderThanDays, int archivedBy)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            var oldVisitors = await _unitOfWork.Visitors.GetAsync(v => 
                v.CreatedOn <= cutoffDate && 
                v.IsActive && 
                !v.IsDeleted &&
                v.VisitCount == 0 && // Only archive visitors with no visits
                v.LastVisitDate == null);

            int archivedCount = 0;
            foreach (var visitor in oldVisitors)
            {
                visitor.SoftDelete(archivedBy);
                _unitOfWork.Visitors.Update(visitor);
                archivedCount++;
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Archived {Count} old visitors older than {Days} days by {ArchivedBy}", 
                archivedCount, olderThanDays, archivedBy);

            return archivedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving old visitors");
            throw;
        }
    }

    public async Task<VisitorDto> RestoreVisitorAsync(int visitorId, int restoredBy)
    {
        try
        {
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId);
            if (visitor == null)
            {
                throw new InvalidOperationException($"Visitor with ID '{visitorId}' not found.");
            }

            if (!visitor.IsDeleted)
            {
                throw new InvalidOperationException("Visitor is not deleted.");
            }

            visitor.Restore(restoredBy);
            _unitOfWork.Visitors.Update(visitor);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Visitor restored: {VisitorId} by {RestoredBy}", visitorId, restoredBy);

            return _mapper.Map<VisitorDto>(visitor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring visitor: {VisitorId}", visitorId);
            throw;
        }
    }

    public async Task<byte[]> ExportVisitorsAsync(List<int> visitorIds, string format)
    {
        try
        {
            // Get visitors by IDs
            var visitors = new List<Visitor>();
            foreach (var id in visitorIds)
            {
                var visitor = await _unitOfWork.Visitors.GetByIdAsync(id);
                if (visitor != null)
                {
                    visitors.Add(visitor);
                }
            }

            // Export based on format
            return format.ToUpper() switch
            {
                "CSV" => ExportToCsv(visitors),
                "EXCEL" => ExportToExcel(visitors),
                "JSON" => ExportToJson(visitors),
                _ => throw new ArgumentException($"Unsupported export format: {format}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting visitors");
            throw;
        }
    }

    public async Task<bool> AnonymizeVisitorDataAsync(int visitorId, int anonymizedBy)
    {
        try
        {
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(visitorId);
            if (visitor == null)
            {
                throw new InvalidOperationException($"Visitor with ID '{visitorId}' not found.");
            }

            // Anonymize personal data
            visitor.FirstName = "Anonymized";
            visitor.LastName = "User";
            visitor.Email = new Domain.ValueObjects.Email($"anonymized.{visitorId}@example.com");
            visitor.PhoneNumber = null;
            visitor.GovernmentId = null;
            visitor.GovernmentIdType = null;
            visitor.DateOfBirth = null;
            visitor.Address = null;
            visitor.DietaryRequirements = null;
            visitor.AccessibilityRequirements = null;
            visitor.Notes = "Data anonymized for GDPR compliance";
            visitor.ProfilePhotoPath = null;

            visitor.UpdateNormalizedEmail();
            visitor.UpdateModifiedBy(anonymizedBy);

            _unitOfWork.Visitors.Update(visitor);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Visitor data anonymized: {VisitorId} by {AnonymizedBy}", 
                visitorId, anonymizedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error anonymizing visitor data: {VisitorId}", visitorId);
            throw;
        }
    }

    public Task<ValidationResult> ValidateBusinessRulesAsync(Visitor visitor, VisitorOperation operation)
    {
        try
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // Common validations
            var commonErrors = visitor.ValidateVisitor();
            errors.AddRange(commonErrors);

            // Operation-specific validations
            switch (operation)
            {
                case VisitorOperation.Blacklist:
                    if (visitor.IsBlacklisted)
                    {
                        errors.Add("Visitor is already blacklisted.");
                    }
                    break;

                case VisitorOperation.RemoveBlacklist:
                    if (!visitor.IsBlacklisted)
                    {
                        errors.Add("Visitor is not blacklisted.");
                    }
                    break;

                case VisitorOperation.MarkAsVip:
                    if (visitor.IsVip)
                    {
                        warnings.Add("Visitor is already marked as VIP.");
                    }
                    if (visitor.IsBlacklisted)
                    {
                        errors.Add("Cannot mark blacklisted visitor as VIP.");
                    }
                    break;

                case VisitorOperation.RemoveVipStatus:
                    if (!visitor.IsVip)
                    {
                        warnings.Add("Visitor is not marked as VIP.");
                    }
                    break;
            }

            var result = new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors,
                Warnings = warnings
            };

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating business rules for visitor: {VisitorId}", visitor.Id);
            return Task.FromResult(ValidationResult.Failure("Validation failed due to system error."));
        }
    }

    #region Private Helper Methods

    private async Task RemoveExistingPhoto(string photoPath)
    {
        try
        {
            var fullPath = Path.Combine(_environment.WebRootPath, photoPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                _logger.LogInformation("Removed existing photo: {PhotoPath}", photoPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove existing photo: {PhotoPath}", photoPath);
            // Don't throw - continue with upload even if old photo can't be deleted
        }
    }

    private List<string> ValidateEmergencyContacts(List<CreateEmergencyContactDto> contacts)
    {
        var errors = new List<string>();

        if (contacts.Any())
        {
            var primaryContacts = contacts.Where(c => c.IsPrimary).ToList();
            if (primaryContacts.Count > 1)
            {
                errors.Add("Only one emergency contact can be marked as primary.");
            }

            // Validate priorities are unique
            var priorities = contacts.Select(c => c.Priority).ToList();
            if (priorities.Count != priorities.Distinct().Count())
            {
                errors.Add("Emergency contact priorities must be unique.");
            }
        }

        return errors;
    }

    private byte[] ExportToCsv(List<Visitor> visitors)
    {
        // Simplified CSV export - in production, use a proper CSV library
        var csv = new List<string>
        {
            "Id,FirstName,LastName,Email,Company,PhoneNumber,IsVip,IsBlacklisted,VisitCount,CreatedOn"
        };

        foreach (var visitor in visitors)
        {
            csv.Add($"{visitor.Id},{visitor.FirstName},{visitor.LastName},{visitor.Email.Value}," +
                   $"{visitor.Company},{visitor.PhoneNumber?.FormattedValue},{visitor.IsVip}," +
                   $"{visitor.IsBlacklisted},{visitor.VisitCount},{visitor.CreatedOn:yyyy-MM-dd}");
        }

        return System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, csv));
    }

    private byte[] ExportToExcel(List<Visitor> visitors)
    {
        // For now, provide CSV export as Excel alternative
        // In production, implement with EPPlus or similar library
        _logger.LogWarning("Excel export not available, providing CSV export instead");
        return ExportToCsv(visitors);
    }

    private byte[] ExportToJson(List<Visitor> visitors)
    {
        var visitorDtos = _mapper.Map<List<VisitorDto>>(visitors);
        var json = System.Text.Json.JsonSerializer.Serialize(visitorDtos, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    #endregion
}
