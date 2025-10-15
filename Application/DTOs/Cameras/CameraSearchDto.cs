using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Cameras;

/// <summary>
/// Data transfer object for camera search and filtering operations
/// Supports advanced querying with pagination and sorting capabilities
/// </summary>
public class CameraSearchDto
{
    /// <summary>
    /// Search term for camera name, description, or connection string
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by camera type
    /// </summary>
    public CameraType? CameraType { get; set; }

    /// <summary>
    /// Filter by camera status
    /// </summary>
    public CameraStatus? Status { get; set; }

    /// <summary>
    /// Filter by location ID
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filter by facial recognition enablement
    /// </summary>
    public bool? EnableFacialRecognition { get; set; }

    /// <summary>
    /// Filter by minimum priority level
    /// </summary>
    public int? MinPriority { get; set; }

    /// <summary>
    /// Filter by maximum priority level
    /// </summary>
    public int? MaxPriority { get; set; }

    /// <summary>
    /// Filter by minimum failure count
    /// </summary>
    public int? MinFailureCount { get; set; }

    /// <summary>
    /// Filter by maximum failure count
    /// </summary>
    public int? MaxFailureCount { get; set; }

    /// <summary>
    /// Filter by manufacturer
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Filter by model
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Filter cameras created after this date
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter cameras created before this date
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Filter cameras last modified after this date
    /// </summary>
    public DateTime? ModifiedFrom { get; set; }

    /// <summary>
    /// Filter cameras last modified before this date
    /// </summary>
    public DateTime? ModifiedTo { get; set; }

    /// <summary>
    /// Filter cameras last seen online after this date
    /// </summary>
    public DateTime? LastOnlineFrom { get; set; }

    /// <summary>
    /// Filter cameras last seen online before this date
    /// </summary>
    public DateTime? LastOnlineTo { get; set; }

    /// <summary>
    /// Include deleted cameras in search results
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>
    /// Page index for pagination (0-based)
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Field to sort by
    /// </summary>
    public string SortBy { get; set; } = "Name";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "asc";
}