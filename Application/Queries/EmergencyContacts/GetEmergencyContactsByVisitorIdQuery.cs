using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Queries.EmergencyContacts;

/// <summary>
/// Query to get emergency contacts by visitor ID
/// </summary>
public class GetEmergencyContactsByVisitorIdQuery : IRequest<List<EmergencyContactDto>>
{
    /// <summary>
    /// Visitor ID
    /// </summary>
    [Required]
    public int VisitorId { get; set; }

    /// <summary>
    /// Include deleted contacts
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}
