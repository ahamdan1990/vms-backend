// Application/Commands/Users/ImportUsersCommand.cs
using MediatR;
using Microsoft.AspNetCore.Http;
using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Command to import users from an uploaded Excel or CSV file.
/// Each valid row is dispatched through the existing CreateUserCommand pipeline.
/// </summary>
public class ImportUsersCommand : IRequest<ImportUsersResultDto>
{
    /// <summary>The uploaded .xlsx or .csv file.</summary>
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// When true, rows that fail validation are skipped and the remaining
    /// valid rows are imported. When false, any validation error aborts the
    /// entire import before touching the database.
    /// </summary>
    public bool SkipInvalidRows { get; set; } = true;

    /// <summary>
    /// When true, the file is fully parsed and validated but NO users are created.
    /// Returns the same result shape so the UI can show a preview.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>The user ID of the administrator performing the import.</summary>
    public int ImportedBy { get; set; }
}
