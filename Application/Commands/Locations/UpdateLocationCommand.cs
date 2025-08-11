using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Locations;

namespace VisitorManagementSystem.Api.Application.Commands.Locations;

/// <summary>
/// Command to update an existing location
/// </summary>
public class UpdateLocationCommand : IRequest<LocationDto>
{
    /// <summary>
    /// Location ID to update
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Location name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Location code (unique identifier)
    /// </summary>
    [MaxLength(20)]
    public string? Code { get; set; }

    /// <summary>
    /// Location description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Location type (e.g., "Office", "Conference Room", "Building")
    /// </summary>
    [MaxLength(50)]
    public string? LocationType { get; set; }

    /// <summary>
    /// Floor number or identifier
    /// </summary>
    [MaxLength(10)]
    public string? Floor { get; set; }

    /// <summary>
    /// Building name or identifier
    /// </summary>
    [MaxLength(50)]
    public string? Building { get; set; }

    /// <summary>
    /// Zone or area identifier
    /// </summary>
    [MaxLength(50)]
    public string? Zone { get; set; }

    /// <summary>
    /// Parent location ID (for hierarchical locations)
    /// </summary>
    public int? ParentLocationId { get; set; }

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Maximum capacity for the location
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? MaxCapacity { get; set; }

    /// <summary>
    /// Whether this location requires escort
    /// </summary>
    public bool RequiresEscort { get; set; } = false;

    /// <summary>
    /// Access level required for this location
    /// </summary>
    [MaxLength(50)]
    public string? AccessLevel { get; set; }

    /// <summary>
    /// Whether this location is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User updating the location
    /// </summary>
    public int UpdatedBy { get; set; }
}
