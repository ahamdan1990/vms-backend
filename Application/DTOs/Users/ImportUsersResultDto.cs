// Application/DTOs/Users/ImportUsersResultDto.cs
namespace VisitorManagementSystem.Api.Application.DTOs.Users;

/// <summary>
/// The overall result returned after executing a user import.
/// </summary>
public class ImportUsersResultDto
{
    public int TotalRows { get; set; }
    public int CreatedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }

    /// <summary>
    /// Per-row results. Includes valid, failed, and skipped rows.
    /// Always returned so the client can build an error-report download.
    /// </summary>
    public List<ImportUserRowResultDto> Results { get; set; } = [];
}

/// <summary>
/// Result for a single import row.
/// </summary>
public class ImportUserRowResultDto
{
    /// <summary>1-based row number matching the source file.</summary>
    public int RowNumber { get; set; }

    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Role { get; set; }

    public ImportRowStatus Status { get; set; }

    /// <summary>
    /// Top-level error message (for simple display).
    /// Non-null only when Status is Failed or Skipped.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Column-level validation errors.</summary>
    public List<ImportFieldErrorDto> FieldErrors { get; set; } = [];

    /// <summary>User ID assigned by the database after successful creation.</summary>
    public int? CreatedUserId { get; set; }
}

/// <summary>
/// A single field-level validation error.
/// </summary>
public class ImportFieldErrorDto
{
    public string ColumnName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public enum ImportRowStatus
{
    Created,
    Skipped,   // Had validation errors and skipInvalidRows=true
    Failed     // Passed validation but failed during creation (race condition, DB error)
}
