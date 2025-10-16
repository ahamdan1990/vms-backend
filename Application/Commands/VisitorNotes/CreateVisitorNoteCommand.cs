using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorNotes;

/// <summary>
/// Command to create a new visitor note
/// </summary>
public class CreateVisitorNoteCommand : IRequest<VisitorNoteDto>
{
    /// <summary>
    /// Visitor ID this note belongs to
    /// </summary>
    [Required]
    public int VisitorId { get; set; }

    /// <summary>
    /// Note title
    /// </summary>
    [Required]
    [MaxLength(200)]
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
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Note priority level
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Whether note is flagged for attention
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
    /// Additional tags
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// User creating the note
    /// </summary>
    public int CreatedBy { get; set; }
}
