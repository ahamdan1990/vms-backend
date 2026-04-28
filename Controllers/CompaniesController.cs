using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.Companies;
using VisitorManagementSystem.Api.Application.DTOs.Companies;
using VisitorManagementSystem.Api.Application.Queries.Companies;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(IMediator mediator, IUnitOfWork unitOfWork, ILogger<CompaniesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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
    [Authorize(Policy = Permissions.Company.Read)]
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
    [Authorize(Policy = Permissions.Company.Read)]
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
    [Authorize(Policy = Permissions.Company.Read)]
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
    [Authorize(Policy = Permissions.Company.Create)]
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
    [Authorize(Policy = Permissions.Company.Update)]
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
    /// Mark a company as verified.
    /// </summary>
    [HttpPut("{id}/verify")]
    [Authorize(Policy = Permissions.Company.Update)]
    public async Task<IActionResult> VerifyCompany(int id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return UnauthorizedResponse();

            var company = await _unitOfWork.Repository<Domain.Entities.Company>()
                .GetByIdAsync(id, cancellationToken);

            if (company == null) return NotFoundResponse("Company", id);

            company.IsVerified = true;
            company.VerifiedOn = DateTime.UtcNow;
            company.VerifiedBy = userId.Value;

            _unitOfWork.Repository<Domain.Entities.Company>().Update(company);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Company {Id} verified by user {UserId}", id, userId);
            return SuccessResponse(new { company.Id, company.IsVerified, company.VerifiedOn });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying company {Id}", id);
            return BadRequestResponse("Failed to verify company", ex.Message);
        }
    }

    /// <summary>
    /// Delete a company
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="reason">Optional deletion reason</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.Company.Delete)]
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
