using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Departments;

namespace VisitorManagementSystem.Api.Application.Commands.Departments;

/// <summary>
/// Command to create a new department
/// </summary>
public class CreateDepartmentCommand : IRequest<CreateDepartmentResult>
{
    /// <summary>
    /// Department creation data
    /// </summary>
    public CreateDepartmentDto DepartmentData { get; set; } = null!;
}

/// <summary>
/// Result of creating a department
/// </summary>
public class CreateDepartmentResult
{
    /// <summary>
    /// Whether the creation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if creation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Created department data
    /// </summary>
    public DepartmentDto? Department { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
