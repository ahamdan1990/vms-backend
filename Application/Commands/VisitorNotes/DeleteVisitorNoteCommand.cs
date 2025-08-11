using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorNotes;

/// <summary>
/// Command to delete a visitor note
/// </summary>
public class DeleteVisitorNoteCommand : IRequest<CommandResultDto<bool>>
{
    /// <summary>
    /// Note ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// User deleting the note
    /// </summary>
    public int DeletedBy { get; set; }

    /// <summary>
    /// Whether to permanently delete (vs soft delete)
    /// </summary>
    public bool PermanentDelete { get; set; } = false;
}
