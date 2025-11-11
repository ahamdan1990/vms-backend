using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Companies;

namespace VisitorManagementSystem.Api.Application.Commands.Companies;

/// <summary>
/// Command to create a new company
/// </summary>
public class CreateCompanyCommand : IRequest<CreateCompanyResult>
{
    /// <summary>
    /// Company creation data
    /// </summary>
    public CreateCompanyDto CompanyData { get; set; } = null!;
}

/// <summary>
/// Result of creating a company
/// </summary>
public class CreateCompanyResult
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
    /// Created company data
    /// </summary>
    public CompanyDto? Company { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
