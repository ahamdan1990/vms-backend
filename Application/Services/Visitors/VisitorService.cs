using AutoMapper;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
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

    public VisitorService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<VisitorService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
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

    public async Task<ValidationResult> ValidateBusinessRulesAsync(Visitor visitor, VisitorOperation operation)
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

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating business rules for visitor: {VisitorId}", visitor.Id);
            return ValidationResult.Failure("Validation failed due to system error.");
        }
    }

    #region Private Helper Methods

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
