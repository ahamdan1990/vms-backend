using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.Departments;
using VisitorManagementSystem.Api.Application.DTOs.Departments;
using VisitorManagementSystem.Api.Application.Queries.Departments;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// API controller for department management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(IMediator mediator, ILogger<DepartmentsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all departments with optional hierarchical filtering
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="parentDepartmentId">Optional parent department ID filter</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="sortBy">Sort field (Name, Code, CreatedOn, DisplayOrder)</param>
    /// <param name="sortDirection">Sort direction (asc, desc)</param>
    /// <returns>Paginated list of departments</returns>
    [HttpGet]
    public async Task<IActionResult> GetDepartments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? parentDepartmentId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string sortBy = "DisplayOrder",
        [FromQuery] string sortDirection = "asc")
    {
        var query = new GetDepartmentsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ParentDepartmentId = parentDepartmentId,
            SearchTerm = searchTerm,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Get a specific department by ID
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <param name="includeChildren">Whether to include child departments</param>
    /// <returns>Department details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDepartmentById(int id, [FromQuery] bool includeChildren = false)
    {
        var query = new GetDepartmentByIdQuery(id, includeChildren);
        var result = await _mediator.Send(query);

        if (result == null || (result.GetType().GetProperty("IsSuccess")?.GetValue(result) is bool isSuccess && !isSuccess))
        {
            return NotFoundResponse("Department", id);
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Create a new department
    /// </summary>
    /// <param name="createDepartmentDto">Department creation data</param>
    /// <returns>Created department</returns>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentDto createDepartmentDto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError(GetModelStateErrors(), "Validation failed");
        }

        var command = new CreateDepartmentCommand { DepartmentData = createDepartmentDto };
        var result = await _mediator.Send(command);

        return CreatedResponse(result, Url.Action(nameof(GetDepartmentById), new { id = result.Department?.Id }));
    }

    /// <summary>
    /// Update an existing department
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <param name="updateDepartmentDto">Update data</param>
    /// <returns>Updated department</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto updateDepartmentDto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError(GetModelStateErrors(), "Validation failed");
        }

        var command = new UpdateDepartmentCommand { Id = id, DepartmentData = updateDepartmentDto };
        var result = await _mediator.Send(command);

        if (result == null || (result.GetType().GetProperty("IsSuccess")?.GetValue(result) is bool isSuccess && !isSuccess))
        {
            return NotFoundResponse("Department", id);
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Delete a department
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <param name="reason">Optional deletion reason</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteDepartment(int id, [FromQuery] string? reason = null)
    {
        try
        {
            var command = new DeleteDepartmentCommand(id, reason);
            var result = await _mediator.Send(command);

            if (result == null || (result.GetType().GetProperty("IsSuccess")?.GetValue(result) is bool isSuccess && !isSuccess))
            {
                return NotFoundResponse("Department", id);
            }

            return SuccessResponse(result, "Department deleted successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete department with ID {DepartmentId}", id);
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department with ID {DepartmentId}", id);
            return BadRequestResponse("An error occurred while deleting the department");
        }
    }
}
