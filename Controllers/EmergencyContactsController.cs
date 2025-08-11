using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.EmergencyContacts;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Queries.EmergencyContacts;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for emergency contact management operations
/// </summary>
[ApiController]
[Route("api/visitors/{visitorId:int}/emergency-contacts")]
[Authorize]
public class EmergencyContactsController : BaseController
{
    private readonly IMediator _mediator;

    public EmergencyContactsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets emergency contacts for a visitor
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="includeDeleted">Include deleted contacts</param>
    /// <returns>List of emergency contacts</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.EmergencyContact.Read)]
    public async Task<IActionResult> GetEmergencyContacts(int visitorId, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetEmergencyContactsByVisitorIdQuery 
        { 
            VisitorId = visitorId,
            IncludeDeleted = includeDeleted
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets an emergency contact by ID
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="id">Emergency contact ID</param>
    /// <param name="includeDeleted">Include deleted contact</param>
    /// <returns>Emergency contact details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.EmergencyContact.Read)]
    public async Task<IActionResult> GetEmergencyContact(int visitorId, int id, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetEmergencyContactByIdQuery 
        { 
            Id = id,
            IncludeDeleted = includeDeleted 
        };

        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFoundResponse("Emergency contact", id);
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Creates a new emergency contact
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="createDto">Emergency contact creation data</param>
    /// <returns>Created emergency contact</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.EmergencyContact.Create)]
    public async Task<IActionResult> CreateEmergencyContact(int visitorId, [FromBody] CreateEmergencyContactDto createDto)
    {
        var command = new CreateEmergencyContactCommand
        {
            VisitorId = visitorId,
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Relationship = createDto.Relationship,
            PhoneNumber = createDto.PhoneNumber,
            AlternatePhoneNumber = createDto.AlternatePhoneNumber,
            Email = createDto.Email,
            Address = createDto.Address,
            Priority = createDto.Priority,
            IsPrimary = createDto.IsPrimary,
            Notes = createDto.Notes,
            CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return CreatedResponse(result, Url.Action(nameof(GetEmergencyContact), new { visitorId, id = result.Id }));
    }

    /// <summary>
    /// Updates an existing emergency contact
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="id">Emergency contact ID</param>
    /// <param name="updateDto">Emergency contact update data</param>
    /// <returns>Updated emergency contact</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.EmergencyContact.Update)]
    public async Task<IActionResult> UpdateEmergencyContact(int visitorId, int id, [FromBody] UpdateEmergencyContactDto updateDto)
    {
        var command = new UpdateEmergencyContactCommand
        {
            Id = id,
            FirstName = updateDto.FirstName,
            LastName = updateDto.LastName,
            Relationship = updateDto.Relationship,
            PhoneNumber = updateDto.PhoneNumber,
            AlternatePhoneNumber = updateDto.AlternatePhoneNumber,
            Email = updateDto.Email,
            Address = updateDto.Address,
            Priority = updateDto.Priority,
            IsPrimary = updateDto.IsPrimary,
            Notes = updateDto.Notes,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Deletes an emergency contact (soft delete)
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="id">Emergency contact ID</param>
    /// <param name="permanentDelete">Whether to permanently delete</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.EmergencyContact.Delete)]
    public async Task<IActionResult> DeleteEmergencyContact(int visitorId, int id, [FromQuery] bool permanentDelete = false)
    {
        var command = new DeleteEmergencyContactCommand
        {
            Id = id,
            DeletedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated"),
            PermanentDelete = permanentDelete
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }
}
