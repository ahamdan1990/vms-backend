using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Application.Commands.EmergencyContacts;

/// <summary>
/// Command to delete an emergency contact
/// </summary>
public class DeleteEmergencyContactCommand : IRequest<CommandResultDto<bool>>
{
    /// <summary>
    /// Emergency contact ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// User deleting the contact
    /// </summary>
    public int DeletedBy { get; set; }

    /// <summary>
    /// Whether to permanently delete (vs soft delete)
    /// </summary>
    public bool PermanentDelete { get; set; } = false;
}
