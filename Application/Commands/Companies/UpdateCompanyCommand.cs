using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Companies;

namespace VisitorManagementSystem.Api.Application.Commands.Companies;

/// <summary>
/// Command to update an existing company
/// </summary>
public class UpdateCompanyCommand : IRequest<UpdateCompanyResult>
{
    /// <summary>
    /// Company ID to update
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Update details
    /// </summary>
    public UpdateCompanyDto CompanyData { get; set; } = null!;
}

/// <summary>
/// Result of updating a company
/// </summary>
public class UpdateCompanyResult
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
    /// Updated company data
    /// </summary>
    public CompanyDto? Company { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
