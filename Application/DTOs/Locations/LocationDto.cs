namespace VisitorManagementSystem.Api.Application.DTOs.Locations;

/// <summary>
/// Location data transfer object
/// </summary>
public class LocationDto
{
    /// <summary>
    /// Location ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Location name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Location code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Location description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Location type
    /// </summary>
    public string LocationType { get; set; } = string.Empty;

    /// <summary>
    /// Floor information
    /// </summary>
    public string? Floor { get; set; }

    /// <summary>
    /// Building information
    /// </summary>
    public string? Building { get; set; }

    /// <summary>
    /// Zone information
    /// </summary>
    public string? Zone { get; set; }

    /// <summary>
    /// Parent location ID
    /// </summary>
    public int? ParentLocationId { get; set; }

    /// <summary>
    /// Parent location name
    /// </summary>
    public string? ParentLocationName { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Maximum capacity
    /// </summary>
    public int MaxCapacity { get; set; }

    /// <summary>
    /// Whether location requires escort
    /// </summary>
    public bool RequiresEscort { get; set; }

    /// <summary>
    /// Required access level
    /// </summary>
    public string AccessLevel { get; set; } = string.Empty;

    /// <summary>
    /// Whether location is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Created by user ID
    /// </summary>
    public int? CreatedBy { get; set; }

    /// <summary>
    /// Modified by user ID
    /// </summary>
    public int? ModifiedBy { get; set; }

    /// <summary>
    /// Created by user name
    /// </summary>
    public string? CreatedByUser { get; set; }

    /// <summary>
    /// Modified by user name
    /// </summary>
    public string? ModifiedByUser { get; set; }

    /// <summary>
    /// Modification date
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Whether this location is soft deleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Deletion date
    /// </summary>
    public DateTime? DeletedOn { get; set; }

    /// <summary>
    /// Deleted by user ID
    /// </summary>
    public int? DeletedBy { get; set; }

    /// <summary>
    /// Deleted by user name
    /// </summary>
    public string? DeletedByUser { get; set; }

    /// <summary>
    /// Related invitations count
    /// </summary>
    public int InvitationsCount { get; set; }

    /// <summary>
    /// Child locations
    /// </summary>
    public List<LocationDto> ChildLocations { get; set; } = new();
}
