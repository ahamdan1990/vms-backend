using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Visitors;

/// <summary>
/// Service for converting visitor special requirements into structured VisitorNote entries
/// This bridges the gap between form data and the advanced notes system
/// </summary>
public interface IVisitorNotesBridgeService
{
    /// <summary>
    /// Creates VisitorNote entries from visitor special requirements during creation
    /// </summary>
    Task CreateNotesFromRequirementsAsync(Visitor visitor, int createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates VisitorNote entries when visitor special requirements change during update
    /// </summary>
    Task UpdateNotesFromRequirementsAsync(Visitor visitor, Visitor previousVisitor, int modifiedBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of visitor notes bridge service
/// </summary>
public class VisitorNotesBridgeService : IVisitorNotesBridgeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VisitorNotesBridgeService> _logger;

    // Note categories for different requirement types
    private const string DIETARY_CATEGORY = "Dietary";
    private const string ACCESSIBILITY_CATEGORY = "Accessibility";
    private const string GENERAL_CATEGORY = "General";
    private const string SECURITY_CATEGORY = "Security";

    public VisitorNotesBridgeService(
        IUnitOfWork unitOfWork,
        ILogger<VisitorNotesBridgeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task CreateNotesFromRequirementsAsync(Visitor visitor, int createdBy, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating notes from requirements for visitor: {VisitorId}", visitor.Id);

        var notesCreated = 0;

        // Create dietary requirements note
        if (!string.IsNullOrWhiteSpace(visitor.DietaryRequirements))
        {
            await CreateVisitorNoteAsync(visitor.Id, "Dietary Requirements", visitor.DietaryRequirements, 
                DIETARY_CATEGORY, "Medium", false, false, createdBy, cancellationToken);
            notesCreated++;
        }

        // Create accessibility requirements note
        if (!string.IsNullOrWhiteSpace(visitor.AccessibilityRequirements))
        {
            await CreateVisitorNoteAsync(visitor.Id, "Accessibility Requirements", visitor.AccessibilityRequirements,
                ACCESSIBILITY_CATEGORY, "Medium", false, false, createdBy, cancellationToken);
            notesCreated++;
        }

        // Create security clearance note if provided
        if (!string.IsNullOrWhiteSpace(visitor.SecurityClearance))
        {
            await CreateVisitorNoteAsync(visitor.Id, "Security Clearance", $"Security clearance level: {visitor.SecurityClearance}",
                SECURITY_CATEGORY, "High", true, true, createdBy, cancellationToken);
            notesCreated++;
        }

        // Create general notes if provided
        if (!string.IsNullOrWhiteSpace(visitor.Notes))
        {
            await CreateVisitorNoteAsync(visitor.Id, "Additional Information", visitor.Notes,
                GENERAL_CATEGORY, "Medium", false, false, createdBy, cancellationToken);
            notesCreated++;
        }

        if (notesCreated > 0)
        {
            _logger.LogInformation("Created {NotesCount} visitor notes from requirements for visitor: {VisitorId}", 
                notesCreated, visitor.Id);
        }
    }

    public async Task UpdateNotesFromRequirementsAsync(Visitor visitor, Visitor previousVisitor, int modifiedBy, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating notes from requirements for visitor: {VisitorId}", visitor.Id);

        // Update dietary requirements note
        await UpdateRequirementNoteAsync(visitor.Id, "Dietary Requirements", 
            previousVisitor.DietaryRequirements, visitor.DietaryRequirements, 
            DIETARY_CATEGORY, modifiedBy, cancellationToken);

        // Update accessibility requirements note
        await UpdateRequirementNoteAsync(visitor.Id, "Accessibility Requirements",
            previousVisitor.AccessibilityRequirements, visitor.AccessibilityRequirements,
            ACCESSIBILITY_CATEGORY, modifiedBy, cancellationToken);

        // Update security clearance note
        await UpdateRequirementNoteAsync(visitor.Id, "Security Clearance",
            $"Security clearance level: {previousVisitor.SecurityClearance}", 
            !string.IsNullOrWhiteSpace(visitor.SecurityClearance) ? $"Security clearance level: {visitor.SecurityClearance}" : null,
            SECURITY_CATEGORY, modifiedBy, cancellationToken, isSecurityNote: true);

        // Update general notes
        await UpdateRequirementNoteAsync(visitor.Id, "Additional Information",
            previousVisitor.Notes, visitor.Notes,
            GENERAL_CATEGORY, modifiedBy, cancellationToken);
    }

    private async Task CreateVisitorNoteAsync(int visitorId, string title, string content, 
        string category, string priority, bool isFlagged, bool isConfidential, 
        int createdBy, CancellationToken cancellationToken)
    {
        var visitorNote = new VisitorNote
        {
            VisitorId = visitorId,
            Title = title,
            Content = content,
            Category = category,
            Priority = priority,
            IsFlagged = isFlagged,
            IsConfidential = isConfidential,
            Tags = $"auto-created,{category.ToLower()},requirements"
        };

        visitorNote.SetCreatedBy(createdBy);
        await _unitOfWork.VisitorNotes.AddAsync(visitorNote, cancellationToken);

        _logger.LogDebug("Created visitor note: {Title} for visitor: {VisitorId}", title, visitorId);
    }

    private async Task UpdateRequirementNoteAsync(int visitorId, string title, string? previousContent, 
        string? newContent, string category, int modifiedBy, CancellationToken cancellationToken, 
        bool isSecurityNote = false)
    {
        // Get existing auto-created note for this requirement type
        var existingNotes = await _unitOfWork.VisitorNotes.GetByVisitorIdAsync(visitorId, cancellationToken);
        var existingNote = existingNotes.FirstOrDefault(n => 
            n.Title == title && 
            n.Category == category && 
            !n.IsDeleted && 
            n.Tags != null && n.Tags.Contains("auto-created"));

        var hadPreviousContent = !string.IsNullOrWhiteSpace(previousContent);
        var hasNewContent = !string.IsNullOrWhiteSpace(newContent);

        if (!hadPreviousContent && hasNewContent)
        {
            // Create new note (requirement was added)
            await CreateVisitorNoteAsync(visitorId, title, newContent, category, 
                isSecurityNote ? "High" : "Medium", isSecurityNote, isSecurityNote, modifiedBy, cancellationToken);
            
            _logger.LogDebug("Created new note for added requirement: {Title} for visitor: {VisitorId}", title, visitorId);
        }
        else if (hadPreviousContent && !hasNewContent && existingNote != null)
        {
            // Remove note (requirement was cleared)
            existingNote.SoftDelete(modifiedBy);
            _unitOfWork.VisitorNotes.Update(existingNote);
            
            _logger.LogDebug("Soft deleted note for removed requirement: {Title} for visitor: {VisitorId}", title, visitorId);
        }
        else if (hadPreviousContent && hasNewContent && existingNote != null && previousContent != newContent)
        {
            // Update existing note (requirement changed)
            existingNote.Content = newContent;
            existingNote.UpdateModifiedBy(modifiedBy);
            _unitOfWork.VisitorNotes.Update(existingNote);
            
            _logger.LogDebug("Updated note for changed requirement: {Title} for visitor: {VisitorId}", title, visitorId);
        }
        else if (hadPreviousContent && hasNewContent && existingNote == null)
        {
            // Edge case: note doesn't exist but should - create it
            await CreateVisitorNoteAsync(visitorId, title, newContent, category,
                isSecurityNote ? "High" : "Medium", isSecurityNote, isSecurityNote, modifiedBy, cancellationToken);
            
            _logger.LogDebug("Recreated missing note for requirement: {Title} for visitor: {VisitorId}", title, visitorId);
        }
    }
}
