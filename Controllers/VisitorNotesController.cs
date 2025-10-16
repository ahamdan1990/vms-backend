using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.VisitorNotes;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Queries.VisitorNotes;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for visitor note management operations
/// </summary>
[ApiController]
[Route("api/visitors/{visitorId:int}/notes")]
[Authorize]
public class VisitorNotesController : BaseController
{
    private readonly IMediator _mediator;

    public VisitorNotesController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets visitor notes for a visitor
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="category">Category filter</param>
    /// <param name="isFlagged">Flagged filter</param>
    /// <param name="isConfidential">Confidential filter</param>
    /// <param name="includeDeleted">Include deleted notes</param>
    /// <returns>List of visitor notes</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.VisitorNote.Read)]
    public async Task<IActionResult> GetVisitorNotes(
        int visitorId,
        [FromQuery] string? category = null,
        [FromQuery] bool? isFlagged = null,
        [FromQuery] bool? isConfidential = null,
        [FromQuery] bool includeDeleted = false)
    {
        var query = new GetVisitorNotesByVisitorIdQuery
        {
            VisitorId = visitorId,
            Category = category,
            IsFlagged = isFlagged,
            IsConfidential = isConfidential,
            IncludeDeleted = includeDeleted
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets a visitor note by ID
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="id">Note ID</param>
    /// <param name="includeDeleted">Include deleted note</param>
    /// <returns>Visitor note details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.VisitorNote.Read)]
    public async Task<IActionResult> GetVisitorNote(int visitorId, int id, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetVisitorNoteByIdQuery
        {
            Id = id,
            IncludeDeleted = includeDeleted
        };

        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFoundResponse("Visitor note", id);
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Creates a new visitor note
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="createDto">Visitor note creation data</param>
    /// <returns>Created visitor note</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.VisitorNote.Create)]
    public async Task<IActionResult> CreateVisitorNote(int visitorId, [FromBody] CreateVisitorNoteDto createDto)
    {
        var command = new CreateVisitorNoteCommand
        {
            VisitorId = visitorId,
            Title = createDto.Title,
            Content = createDto.Content,
            Category = createDto.Category,
            Priority = createDto.Priority,
            IsFlagged = createDto.IsFlagged,
            IsConfidential = createDto.IsConfidential,
            FollowUpDate = createDto.FollowUpDate,
            Tags = createDto.Tags,
            CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return CreatedResponse(result, Url.Action(nameof(GetVisitorNote), new { visitorId, id = result.Id }));
    }

    /// <summary>
    /// Updates an existing visitor note
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="id">Note ID</param>
    /// <param name="updateDto">Visitor note update data</param>
    /// <returns>Updated visitor note</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.VisitorNote.Update)]
    public async Task<IActionResult> UpdateVisitorNote(int visitorId, int id, [FromBody] UpdateVisitorNoteDto updateDto)
    {
        var command = new UpdateVisitorNoteCommand
        {
            Id = id,
            Title = updateDto.Title,
            Content = updateDto.Content,
            Category = updateDto.Category,
            Priority = updateDto.Priority,
            IsFlagged = updateDto.IsFlagged,
            IsConfidential = updateDto.IsConfidential,
            FollowUpDate = updateDto.FollowUpDate,
            Tags = updateDto.Tags,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Deletes a visitor note (soft delete)
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="id">Note ID</param>
    /// <param name="permanentDelete">Whether to permanently delete</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.VisitorNote.Delete)]
    public async Task<IActionResult> DeleteVisitorNote(int visitorId, int id, [FromQuery] bool permanentDelete = false)
    {
        var command = new DeleteVisitorNoteCommand
        {
            Id = id,
            DeletedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated"),
            PermanentDelete = permanentDelete
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }
}
