using MediatR;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.DTOs.Locations;

namespace VisitorManagementSystem.Api.Application.Queries.Locations;

/// <summary>
/// Query to get location by ID
/// </summary>
public class GetLocationByIdQuery : IRequest<LocationDto?>
{
    /// <summary>
    /// Location ID
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Include child locations
    /// </summary>
    public bool IncludeChildren { get; set; } = false;
}
