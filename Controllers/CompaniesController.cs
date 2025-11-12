using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.Companies;
using VisitorManagementSystem.Api.Application.DTOs.Companies;
using VisitorManagementSystem.Api.Application.Queries.Companies;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// API controller for company management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(IMediator mediator, ILogger<CompaniesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all companies with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="isVerified">Optional verification filter</param>
    /// <param name="sortBy">Sort field (Name, Code, CreatedOn, VisitorCount)</param>
    /// <param name="sortDirection">Sort direction (asc, desc)</param>
    /// <returns>Paginated list of companies</returns>
    [HttpGet]
    public async Task<IActionResult> GetCompanies(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isVerified = null,
        [FromQuery] string sortBy = "Name",
        [FromQuery] string sortDirection = "asc")
    {
        var query = new GetCompaniesQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            IsVerified = isVerified,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Get a specific company by ID
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <returns>Company details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCompanyById(int id)
    {
        var query = new GetCompanyByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null || (result.GetType().GetProperty("IsSuccess")?.GetValue(result) is bool isSuccess && !isSuccess))
        {
            return NotFoundResponse("Company", id);
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Search for companies
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="searchField">Field to search (Name, Code, Industry, ContactPersonName, All)</param>
    /// <param name="maxResults">Maximum results to return</param>
    /// <returns>Search results</returns>
    [HttpGet("search")]
    public async Task<IActionResult> SearchCompanies(
        [FromQuery] string searchTerm,
        [FromQuery] string searchField = "All",
        [FromQuery] int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequestResponse("Search term is required");
        }

        var query = new SearchCompaniesQuery
        {
            SearchTerm = searchTerm,
            SearchField = searchField,
            MaxResults = maxResults
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Create a new company
    /// </summary>
    /// <param name="createCompanyDto">Company creation data</param>
    /// <returns>Created company</returns>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyDto createCompanyDto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError(GetModelStateErrors(), "Validation failed");
        }

        var command = new CreateCompanyCommand { CompanyData = createCompanyDto };
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Errors?.Any() == true)
            {
                return ValidationError(result.Errors, result.ErrorMessage ?? "Validation failed");
            }
            return BadRequestResponse(result.ErrorMessage ?? "Failed to create company");
        }

        return CreatedResponse(result.Company, Url.Action(nameof(GetCompanyById), new { id = result.Company?.Id }));
    }

    /// <summary>
    /// Update an existing company
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="updateCompanyDto">Update data</param>
    /// <returns>Updated company</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UpdateCompany(int id, [FromBody] UpdateCompanyDto updateCompanyDto)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError(GetModelStateErrors(), "Validation failed");
        }

        var command = new UpdateCompanyCommand { Id = id, CompanyData = updateCompanyDto };
        var result = await _mediator.Send(command);

        if (result == null || (result.GetType().GetProperty("IsSuccess")?.GetValue(result) is bool isSuccess && !isSuccess))
        {
            return NotFoundResponse("Company", id);
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Delete a company
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="reason">Optional deletion reason</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteCompany(int id, [FromQuery] string? reason = null)
    {
        try
        {
            var command = new DeleteCompanyCommand(id, reason);
            var result = await _mediator.Send(command);

            if (result == null || (result.GetType().GetProperty("IsSuccess")?.GetValue(result) is bool isSuccess && !isSuccess))
            {
                return NotFoundResponse("Company", id);
            }

            return SuccessResponse(result, "Company deleted successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete company with ID {CompanyId}", id);
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting company with ID {CompanyId}", id);
            return BadRequestResponse("An error occurred while deleting the company");
        }
    }
}
