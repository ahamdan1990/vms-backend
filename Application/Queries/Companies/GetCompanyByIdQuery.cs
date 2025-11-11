using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Companies;

namespace VisitorManagementSystem.Api.Application.Queries.Companies;

/// <summary>
/// Query to retrieve a single company by ID
/// </summary>
public class GetCompanyByIdQuery : IRequest<GetCompanyByIdResult>
{
    /// <summary>
    /// Company ID
    /// </summary>
    public int Id { get; set; }

    public GetCompanyByIdQuery(int id)
    {
        Id = id;
    }
}

/// <summary>
/// Result of getting a company by ID
/// </summary>
public class GetCompanyByIdResult
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
    /// The company
    /// </summary>
    public CompanyDto? Company { get; set; }
}
