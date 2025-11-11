using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Departments;

namespace VisitorManagementSystem.Api.Application.Queries.Departments;

/// <summary>
/// Query to retrieve a single department by ID
/// </summary>
public class GetDepartmentByIdQuery : IRequest<GetDepartmentByIdResult>
{
    /// <summary>
    /// Department ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Whether to include child departments
    /// </summary>
    public bool IncludeChildren { get; set; } = false;

    public GetDepartmentByIdQuery(int id, bool includeChildren = false)
    {
        Id = id;
        IncludeChildren = includeChildren;
    }
}

/// <summary>
/// Result of getting a department by ID
/// </summary>
public class GetDepartmentByIdResult
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
    /// The department
    /// </summary>
    public DepartmentDto? Department { get; set; }
}
