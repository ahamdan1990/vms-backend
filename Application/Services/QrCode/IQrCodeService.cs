using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Services.QrCode;

/// <summary>
/// QR code service interface
/// </summary>
public interface IQrCodeService
{
    /// <summary>
    /// Generates QR code image as byte array
    /// </summary>
    /// <param name="data">Data to encode in QR code</param>
    /// <param name="size">Size of the QR code in pixels (default: 300)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>QR code image as PNG byte array</returns>
    Task<byte[]> GenerateQrCodeImageAsync(string data, int size = 300, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates QR code data for an invitation
    /// </summary>
    /// <param name="invitation">Invitation entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>QR code data string</returns>
    Task<string> GenerateInvitationQrDataAsync(Invitation invitation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates QR code data format
    /// </summary>
    /// <param name="qrData">QR code data to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if QR data is valid</returns>
    Task<bool> ValidateQrCodeDataAsync(string qrData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts invitation number from QR code data
    /// </summary>
    /// <param name="qrData">QR code data</param>
    /// <returns>Invitation number if found, null otherwise</returns>
    string? ExtractInvitationNumberFromQrData(string qrData);

    /// <summary>
    /// Generates visitor check-in QR code data
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="invitationId">Invitation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>QR code data for visitor check-in</returns>
    Task<string> GenerateVisitorCheckInQrDataAsync(int visitorId, int invitationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates QR code with logo/branding
    /// </summary>
    /// <param name="data">Data to encode</param>
    /// <param name="logoPath">Path to logo image (optional)</param>
    /// <param name="size">QR code size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Branded QR code image</returns>
    Task<byte[]> GenerateBrandedQrCodeImageAsync(string data, string? logoPath = null, int size = 300, CancellationToken cancellationToken = default);
}

/// <summary>
/// QR code validation result
/// </summary>
public class QrCodeValidationResult
{
    /// <summary>
    /// Whether the QR code is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Extracted invitation number (if valid)
    /// </summary>
    public string? InvitationNumber { get; set; }

    /// <summary>
    /// Extracted visitor ID (if valid)
    /// </summary>
    public int? VisitorId { get; set; }

    /// <summary>
    /// QR code type
    /// </summary>
    public QrCodeType Type { get; set; }

    /// <summary>
    /// Validation error message (if invalid)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static QrCodeValidationResult Success(string invitationNumber, int? visitorId, QrCodeType type)
    {
        return new QrCodeValidationResult
        {
            IsValid = true,
            InvitationNumber = invitationNumber,
            VisitorId = visitorId,
            Type = type
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static QrCodeValidationResult Failure(string errorMessage)
    {
        return new QrCodeValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// QR code types
/// </summary>
public enum QrCodeType
{
    Invitation = 0,
    VisitorCheckIn = 1,
    HostAccess = 2,
    EmergencyAccess = 3
}