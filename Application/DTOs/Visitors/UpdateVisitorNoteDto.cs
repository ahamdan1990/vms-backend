using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// DTO for updating an existing visitor note
/// </summary>
public class UpdateVisitorNoteDto
{
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
    /// Note category
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "General";

    /// <summary>
    /// Priority level
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Whether note is flagged for follow-up
    /// </summary>
    public bool IsFlagged { get; set; } = false;

    /// <summary>
    /// Whether note is confidential
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
}
