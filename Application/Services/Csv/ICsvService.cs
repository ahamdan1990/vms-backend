using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Application.Services.Pdf;

namespace VisitorManagementSystem.Api.Application.Services.Csv;

/// <summary>
/// CSV service interface for invitation workflows
/// </summary>
public interface ICsvService
{
    /// <summary>
    /// Generates a blank CSV template for manual invitation creation
    /// </summary>
    /// <param name="includeMultipleVisitors">Whether to include rows for multiple visitors</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CSV template as byte array</returns>
    Task<byte[]> GenerateInvitationTemplateAsync(bool includeMultipleVisitors = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses a filled CSV invitation file
    /// </summary>
    /// <param name="csvStream">CSV stream to parse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed invitation data</returns>
    Task<ParsedInvitationData> ParseFilledInvitationAsync(Stream csvStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a filled invitation CSV for an existing invitation
    /// </summary>
    /// <param name="invitation">Invitation entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filled CSV as byte array</returns>
    Task<byte[]> GenerateFilledInvitationCsvAsync(Invitation invitation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates CSV structure and required fields
    /// </summary>
    /// <param name="csvStream">CSV stream to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<CsvValidationResult> ValidateCsvStructureAsync(Stream csvStream, CancellationToken cancellationToken = default);
}

/// <summary>
/// CSV validation result
/// </summary>
public class CsvValidationResult
{
    /// <summary>
    /// Whether the CSV is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Number of rows found
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// List of found column headers
    /// </summary>
    public List<string> ColumnHeaders { get; set; } = new();

    /// <summary>
    /// Number of visitor rows detected
    /// </summary>
    public int VisitorRowCount { get; set; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static CsvValidationResult Success(int rowCount, List<string> columnHeaders, int visitorRowCount)
    {
        return new CsvValidationResult
        {
            IsValid = true,
            RowCount = rowCount,
            ColumnHeaders = columnHeaders,
            VisitorRowCount = visitorRowCount
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static CsvValidationResult Failure(List<string> errors)
    {
        return new CsvValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }
}