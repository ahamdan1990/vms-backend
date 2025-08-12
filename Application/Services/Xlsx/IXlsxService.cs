using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Application.Services.Pdf;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Services.Xlsx;

/// <summary>
/// XLSX service interface for invitation workflows with dropdown support
/// </summary>
public interface IXlsxService
{
    /// <summary>
    /// Generates a blank XLSX template with dropdowns for manual invitation creation
    /// </summary>
    /// <param name="includeMultipleVisitors">Whether to include rows for multiple visitors</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>XLSX template as byte array</returns>
    Task<byte[]> GenerateInvitationTemplateAsync(bool includeMultipleVisitors = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses a filled XLSX invitation file
    /// </summary>
    /// <param name="xlsxStream">XLSX stream to parse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed invitation data</returns>
    Task<ParsedInvitationData> ParseFilledInvitationAsync(Stream xlsxStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a filled invitation XLSX for an existing invitation
    /// </summary>
    /// <param name="invitation">Invitation entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filled XLSX as byte array</returns>
    Task<byte[]> GenerateFilledInvitationXlsxAsync(Invitation invitation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates XLSX structure and required fields
    /// </summary>
    /// <param name="xlsxStream">XLSX stream to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<XlsxValidationResult> ValidateXlsxStructureAsync(Stream xlsxStream, CancellationToken cancellationToken = default);
}

/// <summary>
/// XLSX validation result
/// </summary>
public class XlsxValidationResult
{
    /// <summary>
    /// Whether the XLSX is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Number of data rows found
    /// </summary>
    public int DataRowCount { get; set; }

    /// <summary>
    /// List of found worksheets
    /// </summary>
    public List<string> WorksheetNames { get; set; } = new();

    /// <summary>
    /// Number of visitor rows detected
    /// </summary>
    public int VisitorRowCount { get; set; }

    /// <summary>
    /// Whether required worksheets exist
    /// </summary>
    public bool HasRequiredWorksheets { get; set; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static XlsxValidationResult Success(int dataRowCount, List<string> worksheetNames, int visitorRowCount, bool hasRequiredWorksheets)
    {
        return new XlsxValidationResult
        {
            IsValid = true,
            DataRowCount = dataRowCount,
            WorksheetNames = worksheetNames,
            VisitorRowCount = visitorRowCount,
            HasRequiredWorksheets = hasRequiredWorksheets
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static XlsxValidationResult Failure(List<string> errors)
    {
        return new XlsxValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }
}