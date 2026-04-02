// Application/Services/Users/IUserImportService.cs
using Microsoft.AspNetCore.Http;
using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Services.Users;

/// <summary>
/// Handles all file-level operations for the user import feature:
/// parsing, row validation, template generation, and duplicate batch-checking.
/// Business-level orchestration (actually creating users) lives in ImportUsersCommandHandler.
/// </summary>
public interface IUserImportService
{
    /// <summary>
    /// Parses an uploaded .xlsx or .csv file into raw import rows.
    /// Does NOT validate the row data — only reads cells and maps them to DTO fields.
    /// </summary>
    Task<UserImportParseResult> ParseFileAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates parsed rows against all business rules (required fields, lengths,
    /// enum values, email format, phone format, boolean parsing).
    /// Also detects within-file duplicates (email and employeeId).
    /// Does NOT check the database — use CheckDuplicatesAsync for that.
    /// </summary>
    List<ImportUserRowResultDto> ValidateRows(List<ImportUserRowDto> rows);

    /// <summary>
    /// Batch-checks which emails and employeeIds from the provided lists already
    /// exist in the database. Returns a single result with both sets of duplicates.
    /// </summary>
    Task<CheckDuplicatesResultDto> CheckDuplicatesAsync(
        List<string> emails,
        List<string> employeeIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the downloadable .xlsx import template (with dropdowns,
    /// example row, and Instructions sheet).
    /// </summary>
    Task<byte[]> GenerateImportTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a downloadable .csv import template.
    /// </summary>
    Task<byte[]> GenerateCsvImportTemplateAsync(CancellationToken cancellationToken = default);
}

/// <summary>Result from parsing an uploaded file.</summary>
public class UserImportParseResult
{
    public bool Success { get; set; }
    public List<string> FileErrors { get; set; } = [];
    public List<ImportUserRowDto> Rows { get; set; } = [];
    public string DetectedFormat { get; set; } = string.Empty; // "xlsx" or "csv"
    public int TotalDataRows => Rows.Count;
}
