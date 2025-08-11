using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;

namespace VisitorManagementSystem.Api.Application.Commands.VisitPurposes;

/// <summary>
/// Command to create a new visit purpose
/// </summary>
public class CreateVisitPurposeCommand : IRequest<VisitPurposeDto>
{
    /// <summary>
    /// Visit purpose name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Visit purpose description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this purpose requires approval
    /// </summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>
    /// Whether this purpose is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Color code for UI display
    /// </summary>
    [MaxLength(7)]
    public string? ColorCode { get; set; }

    /// <summary>
    /// Icon name for UI display
    /// </summary>
    [MaxLength(50)]
    public string? IconName { get; set; }

    /// <summary>
    /// User creating the visit purpose
    /// </summary>
    public int CreatedBy { get; set; }
}
