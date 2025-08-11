using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents internal notes about a visitor
/// </summary>
public class VisitorNote : SoftDeleteEntity
{
    /// <summary>
    /// Foreign key to the visitor
    /// </summary>
    [Required]
    public int VisitorId { get; set; }

    /// <summary>
    /// Note title/subject
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Note content
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Note category (General, Security, Medical, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "General";

    /// <summary>
    /// Priority level (Low, Medium, High, Critical)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";    /// <summary>
    /// Whether the note is flagged for follow-up
    /// </summary>
    public bool IsFlagged { get; set; } = false;

    /// <summary>
    /// Whether the note is confidential
    /// </summary>
    public bool IsConfidential { get; set; } = false;

    /// <summary>
    /// Follow-up date if flagged
    /// </summary>
    public DateTime? FollowUpDate { get; set; }

    /// <summary>
    /// Tags associated with the note
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Navigation property to the visitor
    /// </summary>
    public virtual Visitor Visitor { get; set; } = null!;

    /// <summary>
    /// Checks if the note requires follow-up and is overdue
    /// </summary>
    /// <returns>True if follow-up is overdue</returns>
    public bool IsFollowUpOverdue()
    {
        return IsFlagged && FollowUpDate.HasValue && FollowUpDate.Value < DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the note information
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateNote()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Note title is required.");

        if (string.IsNullOrWhiteSpace(Content))
            errors.Add("Note content is required.");

        if (IsFlagged && !FollowUpDate.HasValue)
            errors.Add("Follow-up date is required for flagged notes.");

        return errors;
    }
}
