using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.Locations;
using VisitorManagementSystem.Api.Application.DTOs.Locations;
using VisitorManagementSystem.Api.Application.Queries.Locations;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for location management operations
/// </summary>
[ApiController]
[Route("api/locations")]
[Authorize]
public class LocationsController : BaseController
{
    private readonly IMediator _mediator;

    public LocationsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets all locations
    /// </summary>
    /// <param name="locationType">Location type filter</param>
    /// <param name="rootOnly">Only root locations</param>
    /// <param name="includeChildren">Include child locations</param>
    /// <param name="includeInactive">Include inactive locations</param>
    /// <returns>List of locations</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    public async Task<IActionResult> GetLocations(
        [FromQuery] string? locationType = null,
        [FromQuery] bool rootOnly = false,
        [FromQuery] bool includeChildren = true,
        [FromQuery] bool includeInactive = false)
    {
        var query = new GetLocationsQuery
        {
            LocationType = locationType,
            RootOnly = rootOnly,
            IncludeChildren = includeChildren,
            IncludeInactive = includeInactive
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets a specific location by ID
    /// </summary>
    /// <param name="id">Location ID</param>
    /// <param name="includeChildren">Include child locations</param>
    /// <returns>Location details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    public async Task<IActionResult> GetLocation(int id, [FromQuery] bool includeChildren = false)
    {
        var query = new GetLocationByIdQuery 
        { 
            Id = id,
            IncludeChildren = includeChildren
        };
        
        var result = await _mediator.Send(query);
        
        if (result == null)
        {
            return NotFound($"Location with ID {id} not found");
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Creates a new location
    /// </summary>
    /// <param name="createDto">Location creation data</param>
    /// <returns>Created location</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationDto createDto)
    {
        var command = new CreateLocationCommand
        {
            Name = createDto.Name,
            Code = createDto.Code,
            Description = createDto.Description,
            LocationType = createDto.LocationType,
            Floor = createDto.Floor,
            Building = createDto.Building,
            Zone = createDto.Zone,
            ParentLocationId = createDto.ParentLocationId,
            DisplayOrder = createDto.DisplayOrder,
            MaxCapacity = createDto.MaxCapacity,
            RequiresEscort = createDto.RequiresEscort,
            AccessLevel = createDto.AccessLevel,
            CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return CreatedResponse(result, Url.Action(nameof(GetLocation), new { id = result.Id }));
    }

    /// <summary>
    /// Updates an existing location
    /// </summary>
    /// <param name="id">Location ID</param>
    /// <param name="updateDto">Location update data</param>
    /// <returns>Updated location</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationDto updateDto)
    {
        var command = new UpdateLocationCommand
        {
            Id = id,
            Name = updateDto.Name,
            Code = updateDto.Code,
            Description = updateDto.Description,
            LocationType = updateDto.LocationType,
            Floor = updateDto.Floor,
            Building = updateDto.Building,
            Zone = updateDto.Zone,
            ParentLocationId = updateDto.ParentLocationId,
            DisplayOrder = updateDto.DisplayOrder,
            MaxCapacity = updateDto.MaxCapacity,
            RequiresEscort = updateDto.RequiresEscort,
            AccessLevel = updateDto.AccessLevel,
            IsActive = updateDto.IsActive,
            UpdatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Deletes a location
    /// </summary>
    /// <param name="id">Location ID</param>
    /// <param name="hardDelete">Whether to perform hard delete (default: false)</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.SystemConfig.Delete)]
    public async Task<IActionResult> DeleteLocation(int id, [FromQuery] bool hardDelete = false)
    {
        var command = new DeleteLocationCommand
        {
            Id = id,
            SoftDelete = !hardDelete,
            DeletedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        
        if (!result)
        {
            return NotFound($"Location with ID {id} not found");
        }

        return SuccessResponse("Location deleted successfully");
    }
}
