using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Departments;

namespace VisitorManagementSystem.Api.Application.Commands.Departments;

/// <summary>
/// Command to update an existing department
/// </summary>
public class UpdateDepartmentCommand : IRequest<UpdateDepartmentResult>
{
    /// <summary>
    /// Department ID to update
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Update details
    /// </summary>
    public UpdateDepartmentDto DepartmentData { get; set; } = null!;
}

/// <summary>
/// Result of updating a department
/// </summary>
public class UpdateDepartmentResult
{
    /// <summary>
    /// Whether the update was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if update failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Updated department data
    /// </summary>
    public DepartmentDto? Department { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
