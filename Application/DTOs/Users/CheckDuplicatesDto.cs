// Application/DTOs/Users/CheckDuplicatesDto.cs
namespace VisitorManagementSystem.Api.Application.DTOs.Users;

/// <summary>
/// Request body for the batch duplicate-check endpoint.
/// Sent before the actual import to give the UI early feedback.
/// </summary>
public class CheckDuplicatesRequestDto
{
    public List<string> Emails { get; set; } = [];
    public List<string> EmployeeIds { get; set; } = [];
}

/// <summary>
/// Response from the batch duplicate-check endpoint.
/// </summary>
public class CheckDuplicatesResultDto
{
    /// <summary>Emails that already exist in the database.</summary>
    public List<string> DuplicateEmails { get; set; } = [];

    /// <summary>EmployeeIds that already exist in the database.</summary>
    public List<string> DuplicateEmployeeIds { get; set; } = [];
}

/// <summary>
/// Request DTO for the validate-only endpoint.
/// </summary>
public class ImportValidationResultDto
{
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }

    /// <summary>Structural file-level errors (missing columns, empty file, etc.).</summary>
    public List<string> FileErrors { get; set; } = [];

    /// <summary>Per-row validation results (same shape as import results).</summary>
    public List<ImportUserRowResultDto> RowResults { get; set; } = [];
}
