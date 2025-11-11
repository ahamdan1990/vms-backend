using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Departments;

/// <summary>
/// Command to delete (soft delete) a department
/// </summary>
public class DeleteDepartmentCommand : IRequest<DeleteDepartmentResult>
{
    /// <summary>
    /// Department ID to delete
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Reason for deletion
    /// </summary>
    public string? Reason { get; set; }

    public DeleteDepartmentCommand(int id, string? reason = null)
    {
        Id = id;
        Reason = reason;
    }
}

/// <summary>
/// Result of deleting a department
/// </summary>
public class DeleteDepartmentResult
{
    /// <summary>
    /// Whether the deletion was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if deletion failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string? Message { get; set; }
}
