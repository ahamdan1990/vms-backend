using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Companies;

/// <summary>
/// Command to delete (soft delete) a company
/// </summary>
public class DeleteCompanyCommand : IRequest<DeleteCompanyResult>
{
    /// <summary>
    /// Company ID to delete
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Reason for deletion
    /// </summary>
    public string? Reason { get; set; }

    public DeleteCompanyCommand(int id, string? reason = null)
    {
        Id = id;
        Reason = reason;
    }
}

/// <summary>
/// Result of deleting a company
/// </summary>
public class DeleteCompanyResult
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
