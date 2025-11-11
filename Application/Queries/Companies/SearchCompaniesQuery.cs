using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Companies;

namespace VisitorManagementSystem.Api.Application.Queries.Companies;

/// <summary>
/// Query to search for companies by name, code, or industry
/// </summary>
public class SearchCompaniesQuery : IRequest<SearchCompaniesResult>
{
    /// <summary>
    /// Search term
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; set; } = 20;

    /// <summary>
    /// Optional field to search in (Name, Code, Industry, ContactPersonName, All)
    /// </summary>
    public string SearchField { get; set; } = "All";
}

/// <summary>
/// Result of searching companies
/// </summary>
public class SearchCompaniesResult
{
    /// <summary>
    /// Whether the search was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the search failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// List of companies matching the search
    /// </summary>
    public List<CompanyDto> Companies { get; set; } = new();

    /// <summary>
    /// Total number of results found
    /// </summary>
    public int ResultCount { get; set; }
}
