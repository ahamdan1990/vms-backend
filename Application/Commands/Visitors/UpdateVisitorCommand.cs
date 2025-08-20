using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.DTOs.Common;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Command for updating an existing visitor
/// </summary>
public class UpdateVisitorCommand : IRequest<VisitorDto>
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Enhanced phone fields
    public string? PhoneNumber { get; set; }
    public string? PhoneCountryCode { get; set; }
    public string? PhoneType { get; set; }
    
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public AddressDto? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? GovernmentId { get; set; }
    public string? GovernmentIdType { get; set; }
    public string? Nationality { get; set; }
    public string Language { get; set; } = "en-US";
    public string? DietaryRequirements { get; set; }
    public string? AccessibilityRequirements { get; set; }
    public string? SecurityClearance { get; set; }
    public string? Notes { get; set; }
    public string? ExternalId { get; set; }
    public int ModifiedBy { get; set; }
}
