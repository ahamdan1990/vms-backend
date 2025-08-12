using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Services.Pdf;

/// <summary>
/// PDF service interface for invitation workflows
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Generates a blank PDF template for manual invitation creation
    /// </summary>
    /// <param name="includeMultipleVisitors">Whether to include sections for multiple visitors</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF template as byte array</returns>
    Task<byte[]> GenerateInvitationTemplateAsync(bool includeMultipleVisitors = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses a filled PDF invitation form
    /// </summary>
    /// <param name="pdfStream">PDF stream to parse</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed invitation data</returns>
    Task<ParsedInvitationData> ParseFilledInvitationAsync(Stream pdfStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a filled invitation PDF for an existing invitation
    /// </summary>
    /// <param name="invitation">Invitation entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filled PDF as byte array</returns>
    Task<byte[]> GenerateFilledInvitationPdfAsync(Invitation invitation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an invitation summary PDF with QR code
    /// </summary>
    /// <param name="invitation">Invitation entity</param>
    /// <param name="qrCodeBytes">QR code image bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Summary PDF as byte array</returns>
    Task<byte[]> GenerateInvitationSummaryPdfAsync(Invitation invitation, byte[]? qrCodeBytes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates PDF structure and form fields
    /// </summary>
    /// <param name="pdfStream">PDF stream to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<PdfValidationResult> ValidatePdfStructureAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}

/// <summary>
/// Parsed invitation data from PDF
/// </summary>
public class ParsedInvitationData
{
    /// <summary>
    /// Host information
    /// </summary>
    public ParsedHostData Host { get; set; } = new();

    /// <summary>
    /// List of visitors
    /// </summary>
    public List<ParsedVisitorData> Visitors { get; set; } = new();

    /// <summary>
    /// Meeting information
    /// </summary>
    public ParsedMeetingData Meeting { get; set; } = new();

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Whether the parsed data is valid
    /// </summary>
    public bool IsValid => !ValidationErrors.Any();
}

/// <summary>
/// Parsed host data
/// </summary>
public class ParsedHostData
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

/// <summary>
/// Parsed visitor data
/// </summary>
public class ParsedVisitorData
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string GovernmentId { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public ParsedEmergencyContact? EmergencyContact { get; set; }
    
    /// <summary>
    /// Indicates if this is an existing visitor from the system
    /// </summary>
    public bool IsExistingVisitor { get; set; }
    
    /// <summary>
    /// The ID of the existing visitor (if IsExistingVisitor is true)
    /// </summary>
    public int? ExistingVisitorId { get; set; }
}

/// <summary>
/// Parsed emergency contact
/// </summary>
public class ParsedEmergencyContact
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
}

/// <summary>
/// Parsed meeting data
/// </summary>
public class ParsedMeetingData
{
    public string Subject { get; set; } = string.Empty;
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string SpecialInstructions { get; set; } = string.Empty;
    public bool RequiresEscort { get; set; }
    public bool RequiresBadge { get; set; }
    public string ParkingInstructions { get; set; } = string.Empty;
}

/// <summary>
/// PDF validation result
/// </summary>
public class PdfValidationResult
{
    /// <summary>
    /// Whether the PDF is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Whether the PDF has form fields
    /// </summary>
    public bool HasFormFields { get; set; }

    /// <summary>
    /// List of found form field names
    /// </summary>
    public List<string> FormFieldNames { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static PdfValidationResult Success(bool hasFormFields, List<string> formFieldNames)
    {
        return new PdfValidationResult
        {
            IsValid = true,
            HasFormFields = hasFormFields,
            FormFieldNames = formFieldNames
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static PdfValidationResult Failure(List<string> errors)
    {
        return new PdfValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }
}