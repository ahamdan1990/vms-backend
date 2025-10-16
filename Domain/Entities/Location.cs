using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Represents a location within the facility
/// </summary>
public class Location : SoftDeleteEntity
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
    [MaxLength(20)]
    public string? Floor { get; set; }    /// <summary>
    /// Room number or identifier
    /// </summary>
    [MaxLength(20)]
    public string? Room { get; set; }

    /// <summary>
    /// Location type (Office, Conference Room, Lobby, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LocationType { get; set; } = "Office";

    /// <summary>
    /// Zone or area within the facility
    /// </summary>
    [MaxLength(50)]
    public string? Zone { get; set; }

    /// <summary>
    /// Maximum occupancy for the location
    /// </summary>
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
    /// Whether the location requires security clearance
    /// </summary>
    public bool RequiresSecurityClearance { get; set; } = false;

    /// <summary>
    /// Security clearance level required
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
    /// Whether the location is accessible for disabled visitors
    /// </summary>
    public bool IsAccessible { get; set; } = true;

    /// <summary>
    /// GPS coordinates (latitude)
    /// </summary>
    public double? Latitude { get; set; }    /// <summary>
    /// GPS coordinates (longitude)
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; } = 1;

    /// <summary>
    /// Special instructions for accessing the location
    /// </summary>
    [MaxLength(1000)]
    public string? AccessInstructions { get; set; }

    /// <summary>
    /// Parent location ID (for hierarchical locations)
    /// </summary>
    public int? ParentLocationId { get; set; }

    /// <summary>
    /// Navigation property to parent location
    /// </summary>
    public virtual Location? ParentLocation { get; set; }

    /// <summary>
    /// Navigation property to child locations
    /// </summary>
    public virtual ICollection<Location> ChildLocations { get; set; } = new List<Location>();

    /// <summary>
    /// Gets the full location path (Building > Floor > Room)
    /// </summary>
    public string GetFullPath()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(Building))
            parts.Add(Building);
            
        if (!string.IsNullOrWhiteSpace(Floor))
            parts.Add($"Floor {Floor}");
            
        if (!string.IsNullOrWhiteSpace(Room))
            parts.Add($"Room {Room}");
            
        if (parts.Count == 0)
            parts.Add(Name);
            
        return string.Join(" > ", parts);
    }

    /// <summary>
    /// Validates the location information
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateLocation()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Location name is required.");

        if (string.IsNullOrWhiteSpace(Code))
            errors.Add("Location code is required.");

        if (MaxOccupancy < 1)
            errors.Add("Maximum occupancy must be greater than zero.");

        if (RequiresSecurityClearance && string.IsNullOrWhiteSpace(SecurityClearanceLevel))
            errors.Add("Security clearance level is required when clearance is needed.");

        return errors;
    }

    /// <summary>
    /// Navigation property for related invitations
    /// </summary>
    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
}
