using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Queries.VisitPurposes;
using VisitorManagementSystem.Api.Application.Commands.VisitPurposes;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for visit purpose management operations
/// </summary>
[ApiController]
[Route("api/visit-purposes")]
[Authorize]
public class VisitPurposesController : BaseController
{
    private readonly IMediator _mediator;

    public VisitPurposesController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets all visit purposes
    /// </summary>
    /// <param name="requiresApproval">Filter by approval requirement</param>
    /// <param name="includeInactive">Include inactive purposes</param>
    /// <returns>List of visit purposes</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.VisitPurpose.Read)]
    public async Task<IActionResult> GetVisitPurposes(
        [FromQuery] bool? requiresApproval = null,
        [FromQuery] bool includeInactive = false)
    {
        var query = new GetVisitPurposesQuery
        {
            RequiresApproval = requiresApproval,
            IncludeInactive = includeInactive
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets a specific visit purpose by ID
    /// </summary>
    /// <param name="id">Visit purpose ID</param>
    /// <returns>Visit purpose details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.VisitPurpose.Read)]
    public async Task<IActionResult> GetVisitPurpose(int id)
    {
        var query = new GetVisitPurposeByIdQuery { Id = id };
        var result = await _mediator.Send(query);
        
        if (result == null)
        {
            return NotFound($"Visit purpose with ID {id} not found");
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Creates a new visit purpose
    /// </summary>
    /// <param name="createDto">Visit purpose creation data</param>
    /// <returns>Created visit purpose</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.VisitPurpose.Create)]
    public async Task<IActionResult> CreateVisitPurpose([FromBody] CreateVisitPurposeDto createDto)
    {
        var command = new CreateVisitPurposeCommand
        {
            Name = createDto.Name,
            Description = createDto.Description,
            RequiresApproval = createDto.RequiresApproval,
            IsActive = createDto.IsActive,
            DisplayOrder = createDto.DisplayOrder,
            ColorCode = createDto.ColorCode,
            IconName = createDto.IconName,
            CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return CreatedResponse(result, Url.Action(nameof(GetVisitPurpose), new { id = result.Id }));
    }

    /// <summary>
    /// Updates an existing visit purpose
    /// </summary>
    /// <param name="id">Visit purpose ID</param>
    /// <param name="updateDto">Visit purpose update data</param>
    /// <returns>Updated visit purpose</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.VisitPurpose.Update)]
    public async Task<IActionResult> UpdateVisitPurpose(int id, [FromBody] UpdateVisitPurposeDto updateDto)
    {
        var command = new UpdateVisitPurposeCommand
        {
            Id = id,
            Name = updateDto.Name,
            Description = updateDto.Description,
            RequiresApproval = updateDto.RequiresApproval,
            IsActive = updateDto.IsActive,
            DisplayOrder = updateDto.DisplayOrder,
            ColorCode = updateDto.ColorCode,
            IconName = updateDto.IconName,
            UpdatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Deletes a visit purpose
    /// </summary>
    /// <param name="id">Visit purpose ID</param>
    /// <param name="hardDelete">Whether to perform hard delete (default: false)</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.VisitPurpose.Delete)]
    public async Task<IActionResult> DeleteVisitPurpose(int id, [FromQuery] bool hardDelete = false)
    {
        var command = new DeleteVisitPurposeCommand
        {
            Id = id,
            SoftDelete = !hardDelete,
            DeletedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        
        if (!result)
        {
            return NotFound($"Visit purpose with ID {id} not found");
        }

        return SuccessResponse("Visit purpose deleted successfully");
    }
}
