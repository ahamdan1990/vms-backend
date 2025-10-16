namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// Visitor note data transfer object
/// </summary>
public class VisitorNoteDto
{
    /// <summary>
    /// Note ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Visitor ID
    /// </summary>
    public int VisitorId { get; set; }

    /// <summary>
    /// Note title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Note content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Note category
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Priority level
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Whether note is flagged
    /// </summary>
    public bool IsFlagged { get; set; }

    /// <summary>
    /// Whether note is confidential
    /// </summary>
    public bool IsConfidential { get; set; }

    /// <summary>
    /// Follow-up date
    /// </summary>
    public DateTime? FollowUpDate { get; set; }

    /// <summary>
    /// Whether follow-up is overdue
    /// </summary>
    public bool IsFollowUpOverdue { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Created by user name
    /// </summary>
    public string? CreatedByName { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Modified by user name
    /// </summary>
    public string? ModifiedByName { get; set; }

    /// <summary>
    /// Whether note is active
    /// </summary>
    public bool IsActive { get; set; }
}
