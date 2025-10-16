using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Locations;

/// <summary>
/// Data transfer object for creating a new location
/// </summary>
public class CreateLocationDto
{
    /// <summary>
    /// Location name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Location description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Location code/identifier
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Building name/number
    /// </summary>
    [MaxLength(50)]
    public string? Building { get; set; }

    /// <summary>
    /// Floor number or identifier
    /// </summary>
    [MaxLength(10)]
    public string? Floor { get; set; }

    /// <summary>
    /// Room number or identifier
    /// </summary>
    [MaxLength(20)]
    public string? Room { get; set; }

    /// <summary>
    /// Location type (Office, Conference Room, Lobby, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LocationType { get; set; } = string.Empty;

    /// <summary>
    /// Zone or area within the facility
    /// </summary>
    [MaxLength(50)]
    public string? Zone { get; set; }

    /// <summary>
    /// Parent location ID for hierarchical locations
    /// </summary>
    public int? ParentLocationId { get; set; }

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; } = 1;

    /// <summary>
    /// Maximum occupancy allowed
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxOccupancy { get; set; } = 1;

    /// <summary>
    /// Maximum capacity alias for consistency
    /// </summary>
    public int MaxCapacity
    {
        get => MaxOccupancy;
        set => MaxOccupancy = value;
    }

    /// <summary>
    /// Whether visitors require an escort to access this location
    /// </summary>
    public bool RequiresEscort { get; set; } = false;

    /// <summary>
    /// Whether the location is accessible (ADA compliant)
    /// </summary>
    public bool IsAccessible { get; set; } = true;

    /// <summary>
    /// Whether security clearance is required
    /// </summary>
    public bool RequiresSecurityClearance { get; set; } = false;

    /// <summary>
    /// Required security clearance level
    /// </summary>
    [MaxLength(50)]
    public string? SecurityClearanceLevel { get; set; }

    /// <summary>
    /// Access level alias for consistency
    /// </summary>
    public string? AccessLevel
    {
        get => SecurityClearanceLevel;
        set => SecurityClearanceLevel = value;
    }

    /// <summary>
    /// Special access instructions
    /// </summary>
    [MaxLength(1000)]
    public string? AccessInstructions { get; set; }

    /// <summary>
    /// Latitude coordinate for mapping
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate for mapping
    /// </summary>
    public double? Longitude { get; set; }
}

/// <summary>
/// DTO for updating a location
/// </summary>
public class UpdateLocationDto
{
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
}
