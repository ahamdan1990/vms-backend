using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Queries.Cameras;

/// <summary>
/// Query for retrieving a paginated list of cameras with filtering and sorting options
/// Optimized for list views and administrative interfaces
/// </summary>
public class GetCamerasQuery : IRequest<PagedResultDto<CameraListDto>>
{
    /// <summary>
    /// Page index for pagination (0-based)
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// Number of cameras per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Search term for camera name or description
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
    /// Include deleted cameras in results
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>
    /// Field to sort by
    /// </summary>
    public string SortBy { get; set; } = "Name";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "asc";
}