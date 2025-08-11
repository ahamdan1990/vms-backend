using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.Visitors;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Queries.Visitors;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for visitor management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VisitorsController : BaseController
{
    private readonly IMediator _mediator;

    public VisitorsController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets a paginated list of visitors with optional filtering
    /// </summary>
    /// <param name="pageIndex">Page index (0-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="company">Company filter</param>
    /// <param name="isVip">VIP status filter</param>
    /// <param name="isBlacklisted">Blacklisted status filter</param>
    /// <param name="isActive">Active status filter</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction</param>
    /// <param name="includeDeleted">Include deleted visitors</param>
    /// <returns>Paginated list of visitors</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.Visitor.Read)]
    public async Task<IActionResult> GetVisitors(
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? company = null,
        [FromQuery] bool? isVip = null,
        [FromQuery] bool? isBlacklisted = null,
        [FromQuery] bool? isActive = true,
        [FromQuery] string sortBy = "FullName",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] bool includeDeleted = false)
    {
        var query = new GetVisitorsQuery
        {
            PageIndex = pageIndex,
            PageSize = Math.Min(pageSize, 100), // Limit max page size
            SearchTerm = searchTerm,
            Company = company,
            IsVip = isVip,
            IsBlacklisted = isBlacklisted,
            IsActive = isActive,
            SortBy = sortBy,
            SortDirection = sortDirection,
            IncludeDeleted = includeDeleted
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets a visitor by ID
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <param name="includeDeleted">Include deleted visitor</param>
    /// <returns>Visitor details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.Visitor.Read)]
    public async Task<IActionResult> GetVisitor(int id, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetVisitorByIdQuery { Id = id, IncludeDeleted = includeDeleted };
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFoundResponse("Visitor", id);
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Creates a new visitor
    /// </summary>
    /// <param name="createDto">Visitor creation data</param>
    /// <returns>Created visitor</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.Visitor.Create)]
    public async Task<IActionResult> CreateVisitor([FromBody] CreateVisitorDto createDto)
    {
        // Validate phone number format
        if (!string.IsNullOrEmpty(createDto.PhoneNumber) &&
            !PhoneNumber.IsValidPhoneNumber(createDto.PhoneNumber))
        {
            return BadRequestResponse($"Invalid phone number format: {createDto.PhoneNumber}");
        }

        // Validate emergency contact phone numbers
        foreach (var contact in createDto.EmergencyContacts)
        {
            if (string.IsNullOrEmpty(contact.PhoneNumber) ||
                !PhoneNumber.IsValidPhoneNumber(contact.PhoneNumber))
            {
                return BadRequestResponse($"Invalid phone number format for emergency contact {contact.FirstName} {contact.LastName}: {contact.PhoneNumber}");
            }
        }

        var command = new CreateVisitorCommand
        {
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Email = createDto.Email,
            PhoneNumber = createDto.PhoneNumber,
            Company = createDto.Company,
            JobTitle = createDto.JobTitle,
            Address = createDto.Address,
            DateOfBirth = createDto.DateOfBirth,
            GovernmentId = createDto.GovernmentId,
            GovernmentIdType = createDto.GovernmentIdType,
            Nationality = createDto.Nationality,
            Language = createDto.Language,
            DietaryRequirements = createDto.DietaryRequirements,
            AccessibilityRequirements = createDto.AccessibilityRequirements,
            SecurityClearance = createDto.SecurityClearance,
            IsVip = createDto.IsVip,
            Notes = createDto.Notes,
            ExternalId = createDto.ExternalId,
            EmergencyContacts = createDto.EmergencyContacts,
            CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return CreatedResponse(result, Url.Action(nameof(GetVisitor), new { id = result.Id }));
    }

    /// <summary>
    /// Updates an existing visitor
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <param name="updateDto">Visitor update data</param>
    /// <returns>Updated visitor</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.Visitor.Update)]
    public async Task<IActionResult> UpdateVisitor(int id, [FromBody] UpdateVisitorDto updateDto)
    {
        // Validate phone number format
        if (!string.IsNullOrEmpty(updateDto.PhoneNumber) &&
            !PhoneNumber.IsValidPhoneNumber(updateDto.PhoneNumber))
        {
            return BadRequestResponse($"Invalid phone number format: {updateDto.PhoneNumber}");
        }

        var command = new UpdateVisitorCommand
        {
            Id = id,
            FirstName = updateDto.FirstName,
            LastName = updateDto.LastName,
            Email = updateDto.Email,
            PhoneNumber = updateDto.PhoneNumber,
            Company = updateDto.Company,
            JobTitle = updateDto.JobTitle,
            Address = updateDto.Address,
            DateOfBirth = updateDto.DateOfBirth,
            GovernmentId = updateDto.GovernmentId,
            GovernmentIdType = updateDto.GovernmentIdType,
            Nationality = updateDto.Nationality,
            Language = updateDto.Language,
            DietaryRequirements = updateDto.DietaryRequirements,
            AccessibilityRequirements = updateDto.AccessibilityRequirements,
            SecurityClearance = updateDto.SecurityClearance,
            Notes = updateDto.Notes,
            ExternalId = updateDto.ExternalId,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Deletes a visitor (soft delete)
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <param name="permanentDelete">Whether to permanently delete</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.Visitor.Delete)]
    public async Task<IActionResult> DeleteVisitor(int id, [FromQuery] bool permanentDelete = false)
    {
        var command = new DeleteVisitorCommand
        {
            Id = id,
            DeletedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated"),
            PermanentDelete = permanentDelete
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Searches for visitors with advanced filtering
    /// </summary>
    /// <param name="searchDto">Search parameters</param>
    /// <returns>Paginated search results</returns>
    [HttpPost("search")]
    [Authorize(Policy = Permissions.Visitor.Read)]
    public async Task<IActionResult> SearchVisitors([FromBody] VisitorSearchDto searchDto)
    {
        var query = new SearchVisitorsQuery
        {
            SearchTerm = searchDto.SearchTerm,
            Company = searchDto.Company,
            IsVip = searchDto.IsVip,
            IsBlacklisted = searchDto.IsBlacklisted,
            IsActive = searchDto.IsActive,
            Nationality = searchDto.Nationality,
            SecurityClearance = searchDto.SecurityClearance,
            MinVisitCount = searchDto.MinVisitCount,
            MaxVisitCount = searchDto.MaxVisitCount,
            CreatedFrom = searchDto.CreatedFrom,
            CreatedTo = searchDto.CreatedTo,
            LastVisitFrom = searchDto.LastVisitFrom,
            LastVisitTo = searchDto.LastVisitTo,
            PageIndex = searchDto.PageIndex,
            PageSize = Math.Min(searchDto.PageSize, 100),
            SortBy = searchDto.SortBy,
            SortDirection = searchDto.SortDirection,
            IncludeDeleted = searchDto.IncludeDeleted
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Blacklists a visitor
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <param name="reason">Blacklist reason</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:int}/blacklist")]
    [Authorize(Policy = Permissions.Visitor.Blacklist)]
    public async Task<IActionResult> BlacklistVisitor(int id, [FromBody] string reason)
    {
        var command = new BlacklistVisitorCommand
        {
            Id = id,
            Reason = reason,
            BlacklistedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Removes blacklist status from a visitor
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}/blacklist")]
    [Authorize(Policy = Permissions.Visitor.RemoveBlacklist)]
    public async Task<IActionResult> RemoveBlacklist(int id)
    {
        var command = new RemoveBlacklistCommand
        {
            Id = id,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Marks a visitor as VIP
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:int}/vip")]
    [Authorize(Policy = Permissions.Visitor.MarkAsVip)]
    public async Task<IActionResult> MarkAsVip(int id)
    {
        var command = new MarkAsVipCommand
        {
            Id = id,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Removes VIP status from a visitor
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}/vip")]
    [Authorize(Policy = Permissions.Visitor.RemoveVipStatus)]
    public async Task<IActionResult> RemoveVipStatus(int id)
    {
        var command = new RemoveVipStatusCommand
        {
            Id = id,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets VIP visitors
    /// </summary>
    /// <param name="includeDeleted">Include deleted visitors</param>
    /// <returns>List of VIP visitors</returns>
    [HttpGet("vip")]
    [Authorize(Policy = Permissions.Visitor.Read)]
    public async Task<IActionResult> GetVipVisitors([FromQuery] bool includeDeleted = false)
    {
        var query = new GetVipVisitorsQuery { IncludeDeleted = includeDeleted };
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets blacklisted visitors
    /// </summary>
    /// <param name="includeDeleted">Include deleted visitors</param>
    /// <returns>List of blacklisted visitors</returns>
    [HttpGet("blacklisted")]
    [Authorize(Policy = Permissions.Visitor.Read)]
    public async Task<IActionResult> GetBlacklistedVisitors([FromQuery] bool includeDeleted = false)
    {
        var query = new GetBlacklistedVisitorsQuery { IncludeDeleted = includeDeleted };
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets visitor statistics
    /// </summary>
    /// <param name="includeDeleted">Include deleted visitors in statistics</param>
    /// <returns>Visitor statistics</returns>
    [HttpGet("statistics")]
    [Authorize(Policy = Permissions.Visitor.ViewStatistics)]
    public async Task<IActionResult> GetVisitorStatistics([FromQuery] bool includeDeleted = false)
    {
        var query = new GetVisitorStatsQuery { IncludeDeleted = includeDeleted };
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets visitor's documents
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>List of visitor documents</returns>
    [HttpGet("{id:int}/documents")]
    [Authorize(Policy = Permissions.VisitorDocument.Read)]
    public async Task<IActionResult> GetVisitorDocuments(int id)
    {
        // Redirect to VisitorDocumentsController
        return RedirectToAction("GetVisitorDocuments", "VisitorDocuments", new { visitorId = id });
    }

    /// <summary>
    /// Gets visitor's notes
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>List of visitor notes</returns>
    [HttpGet("{id:int}/notes")]
    [Authorize(Policy = Permissions.VisitorNote.Read)]
    public async Task<IActionResult> GetVisitorNotes(int id)
    {
        // Redirect to VisitorNotesController
        return RedirectToAction("GetVisitorNotes", "VisitorNotes", new { visitorId = id });
    }

    /// <summary>
    /// Gets visitor's emergency contacts
    /// </summary>
    /// <param name="id">Visitor ID</param>
    /// <returns>List of emergency contacts</returns>
    [HttpGet("{id:int}/emergency-contacts")]
    [Authorize(Policy = Permissions.EmergencyContact.Read)]
    public async Task<IActionResult> GetEmergencyContacts(int id)
    {
        // Redirect to EmergencyContactsController
        return RedirectToAction("GetEmergencyContacts", "EmergencyContacts", new { visitorId = id });
    }
}
