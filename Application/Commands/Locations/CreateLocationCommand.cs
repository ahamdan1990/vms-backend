using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Locations;

namespace VisitorManagementSystem.Api.Application.Commands.Locations;

/// <summary>
/// Command to create a new location
/// </summary>
public class CreateLocationCommand : IRequest<LocationDto>
{
    /// <summary>
    /// Location name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Location code
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Location description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Location type
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LocationType { get; set; } = string.Empty;

    /// <summary>
    /// Floor information
    /// </summary>
    [MaxLength(20)]
    public string? Floor { get; set; }

    /// <summary>
    /// Building information
    /// </summary>
    [MaxLength(50)]
    public string? Building { get; set; }

    /// <summary>
    /// Zone information
    /// </summary>
    [MaxLength(50)]
    public string? Zone { get; set; }

    /// <summary>
    /// Parent location ID
    /// </summary>
    public int? ParentLocationId { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Maximum capacity
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MaxCapacity { get; set; } = 0;

    /// <summary>
    /// Whether location requires escort
    /// </summary>
    public bool RequiresEscort { get; set; } = false;

    /// <summary>
    /// Required access level
    /// </summary>
    [MaxLength(20)]
    public string AccessLevel { get; set; } = "Public";

    /// <summary>
    /// User creating the location
    /// </summary>
    public int CreatedBy { get; set; }
}
