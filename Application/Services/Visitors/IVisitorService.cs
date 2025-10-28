using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Visitors;

/// <summary>
/// Service interface for visitor business logic
/// </summary>
public interface IVisitorService
{
    /// <summary>
    /// Validates visitor information before creation
    /// </summary>
    /// <param name="createDto">Visitor creation data</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateVisitorForCreationAsync(CreateVisitorDto createDto);

    /// <summary>
    /// Validates visitor information before update
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <param name="updateDto">Visitor update data</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateVisitorForUpdateAsync(int id, UpdateVisitorDto updateDto);

    /// <summary>
    /// Checks if visitor can be deleted
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateVisitorForDeletionAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges duplicate visitor records
    /// </summary>
    /// <param name="primaryVisitorId">Primary visitor to keep</param>
    /// <param name="duplicateVisitorIds">Duplicate visitors to merge</param>
    /// <param name="mergedBy">User performing the merge</param>
    /// <returns>Merged visitor</returns>
    Task<VisitorDto> MergeVisitorsAsync(int primaryVisitorId, List<int> duplicateVisitorIds, int mergedBy);

    /// <summary>
    /// Finds potential duplicate visitors
    /// </summary>
    /// <param name="visitor">Visitor to check for duplicates</param>
    /// <returns>List of potential duplicates</returns>
    Task<List<VisitorDto>> FindPotentialDuplicatesAsync(Visitor visitor);

    /// <summary>
    /// Uploads and saves a visitor's profile photo
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="file">Photo file to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Photo URL</returns>
    Task<string> UploadVisitorPhotoAsync(int visitorId, IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates visitor's profile photo
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="photoPath">Path to the new photo</param>
    /// <param name="updatedBy">User performing the update</param>
    /// <returns>Updated visitor</returns>
    Task<VisitorDto> UpdateProfilePhotoAsync(int visitorId, string photoPath, int updatedBy);

    /// <summary>
    /// Removes visitor's profile photo
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="updatedBy">User performing the update</param>
    /// <returns>Updated visitor</returns>
    Task<VisitorDto> RemoveProfilePhotoAsync(int visitorId, int updatedBy);

    /// <summary>
    /// Gets visitor's complete profile with all related data
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <returns>Complete visitor profile</returns>
    Task<VisitorDto?> GetCompleteVisitorProfileAsync(int visitorId);

    /// <summary>
    /// Archives old visitor records
    /// </summary>
    /// <param name="olderThanDays">Archive visitors older than specified days</param>
    /// <param name="archivedBy">User performing the archival</param>
    /// <returns>Number of archived visitors</returns>
    Task<int> ArchiveOldVisitorsAsync(int olderThanDays, int archivedBy);

    /// <summary>
    /// Restores archived visitor records
    /// </summary>
    /// <param name="visitorId">Visitor ID to restore</param>
    /// <param name="restoredBy">User performing the restoration</param>
    /// <returns>Restored visitor</returns>
    Task<VisitorDto> RestoreVisitorAsync(int visitorId, int restoredBy);

    /// <summary>
    /// Exports visitor data
    /// </summary>
    /// <param name="visitorIds">List of visitor IDs to export</param>
    /// <param name="format">Export format (CSV, Excel, JSON)</param>
    /// <returns>Exported data as byte array</returns>
    Task<byte[]> ExportVisitorsAsync(List<int> visitorIds, string format);

    /// <summary>
    /// Anonymizes visitor data for GDPR compliance
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="anonymizedBy">User performing the anonymization</param>
    /// <returns>Success result</returns>
    Task<bool> AnonymizeVisitorDataAsync(int visitorId, int anonymizedBy);

    /// <summary>
    /// Validates business rules for visitor operations
    /// </summary>
    /// <param name="visitor">Visitor entity</param>
    /// <param name="operation">Operation being performed</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateBusinessRulesAsync(Visitor visitor, VisitorOperation operation);
}

/// <summary>
/// Validation result for visitor operations
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }
    
    public static ValidationResult Failure(params string[] errors)
    {
        return new ValidationResult 
        { 
            IsValid = false, 
            Errors = errors.ToList() 
        };
    }
}

/// <summary>
/// Visitor operation types
/// </summary>
public enum VisitorOperation
{
    Create,
    Update,
    Delete,
    Blacklist,
    RemoveBlacklist,
    MarkAsVip,
    RemoveVipStatus
}
