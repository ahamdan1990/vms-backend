using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Locations;

namespace VisitorManagementSystem.Api.Application.Queries.Locations;

/// <summary>
/// Query to get all locations
/// </summary>
public class GetLocationsQuery : IRequest<List<LocationDto>>
{
    /// <summary>
    /// Location type filter
    /// </summary>
    public string? LocationType { get; set; }

    /// <summary>
    /// Only root locations (no parent)
    /// </summary>
    public bool RootOnly { get; set; } = false;

    /// <summary>
    /// Include child locations
    /// </summary>
    public bool IncludeChildren { get; set; } = true;

    /// <summary>
    /// Include inactive locations
    /// </summary>
    public bool IncludeInactive { get; set; } = false;
}
