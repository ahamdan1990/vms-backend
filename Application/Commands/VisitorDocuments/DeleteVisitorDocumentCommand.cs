using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Application.Commands.VisitorDocuments;

/// <summary>
/// Command to delete a visitor document
/// </summary>
public class DeleteVisitorDocumentCommand : IRequest<CommandResultDto<bool>>
{
    /// <summary>
    /// Document ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// User deleting the document
    /// </summary>
    public int DeletedBy { get; set; }

    /// <summary>
    /// Whether to permanently delete (vs soft delete)
    /// </summary>
    public bool PermanentDelete { get; set; } = false;
}
