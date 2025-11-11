using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Departments;

namespace VisitorManagementSystem.Api.Application.Queries.Departments;

/// <summary>
/// Query to retrieve all departments with optional hierarchical filtering
/// </summary>
public class GetDepartmentsQuery : IRequest<GetDepartmentsResult>
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
    /// Optional parent department ID to get only child departments
    /// </summary>
    public int? ParentDepartmentId { get; set; }

    /// <summary>
    /// Optional search term to filter by department name or code
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Sort field (Name, Code, CreatedOn, DisplayOrder)
    /// </summary>
    public string SortBy { get; set; } = "DisplayOrder";

    /// <summary>
    /// Sort direction (asc, desc)
    /// </summary>
    public string SortDirection { get; set; } = "asc";
}

/// <summary>
/// Result of getting departments
/// </summary>
public class GetDepartmentsResult
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
    /// List of departments
    /// </summary>
    public List<DepartmentDto> Departments { get; set; } = new();

    /// <summary>
    /// Total number of departments
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
