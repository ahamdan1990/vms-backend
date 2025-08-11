using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.EmergencyContacts;

/// <summary>
/// Query to get an emergency contact by ID
/// </summary>
public class GetEmergencyContactByIdQuery : IRequest<EmergencyContactDto?>
{
    /// <summary>
    /// Emergency contact ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Include deleted contact
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}
