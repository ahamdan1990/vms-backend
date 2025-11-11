using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Companies;

namespace VisitorManagementSystem.Api.Application.Queries.Companies;

/// <summary>
/// Query to retrieve all companies with pagination
/// </summary>
public class GetCompaniesQuery : IRequest<GetCompaniesResult>
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Optional search term to filter by company name or code
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by verified status
    /// </summary>
    public bool? IsVerified { get; set; }

    /// <summary>
    /// Sort field (Name, Code, CreatedOn, VisitorCount)
    /// </summary>
    public string SortBy { get; set; } = "Name";

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    public string SortDirection { get; set; } = "asc";
}

/// <summary>
/// Result of getting companies
/// </summary>
public class GetCompaniesResult
{
    /// <summary>
    /// Whether the query was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the query failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// List of companies
    /// </summary>
    public List<CompanyDto> Companies { get; set; } = new();

    /// <summary>
    /// Total number of companies
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }
}
