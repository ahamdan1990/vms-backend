using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using VisitorManagementSystem.Api.Domain.Entities;
using Microsoft.Extensions.Options;
using VisitorManagementSystem.Api.Configuration;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace VisitorManagementSystem.Api.Application.Services.QrCode;

/// <summary>
/// QR code service implementation using QRCoder
/// </summary>
public class QrCodeService : IQrCodeService
{
    private readonly EmailConfiguration _emailConfig;
    private readonly ILogger<QrCodeService> _logger;
    private readonly IWebHostEnvironment _environment;

    // QR code data format patterns
    // Updated to include hyphens in invitation number pattern
    private static readonly Regex InvitationQrPattern = new(@"^INV:([A-Z0-9\-]+):VIS:(\d+):HOST:(\d+)$", RegexOptions.Compiled);
    private static readonly Regex VisitorCheckInPattern = new(@"^CHK:VIS:(\d+):INV:(\d+):TS:(\d+)$", RegexOptions.Compiled);

    public QrCodeService(IOptions<EmailConfiguration> emailConfig, ILogger<QrCodeService> logger, IWebHostEnvironment environment)
    {
        _emailConfig = emailConfig.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task<byte[]> GenerateQrCodeImageAsync(string data, int size = 300, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateQrCodeParameters(data, size);

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            // Calculate pixels per module (minimum 5 pixels per module for readability)
            var pixelsPerModule = Math.Max(5, size / 25);
            var qrImageBytes = qrCode.GetGraphic(pixelsPerModule);

            return await Task.FromResult(qrImageBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate QR code image for data: {Data}", data);
            throw;
        }
    }
    public async Task<string> GenerateInvitationQrDataAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        if (invitation == null)
            throw new ArgumentNullException(nameof(invitation));

        // Format: INV:{InvitationNumber}:VIS:{VisitorId}:HOST:{HostId}
        var qrData = $"INV:{invitation.InvitationNumber}:VIS:{invitation.VisitorId}:HOST:{invitation.HostId}";
        
        _logger.LogDebug("Generated QR data for invitation {InvitationId}: {QrData}", 
            invitation.Id, qrData);

        return await Task.FromResult(qrData);
    }

    public async Task<string> GenerateVisitorCheckInQrDataAsync(int visitorId, int invitationId, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // Format: CHK:VIS:{VisitorId}:INV:{InvitationId}:TS:{Timestamp}
        var qrData = $"CHK:VIS:{visitorId}:INV:{invitationId}:TS:{timestamp}";
        
        _logger.LogDebug("Generated visitor check-in QR data for visitor {VisitorId}, invitation {InvitationId}", 
            visitorId, invitationId);

        return await Task.FromResult(qrData);
    }

    public async Task<bool> ValidateQrCodeDataAsync(string qrData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(qrData))
            return false;

        var validationResult = ParseQrCodeData(qrData);
        return await Task.FromResult(validationResult.IsValid);
    }

    public string? ExtractInvitationNumberFromQrData(string qrData)
    {
        if (string.IsNullOrWhiteSpace(qrData))
            return null;

        var match = InvitationQrPattern.Match(qrData);
        return match.Success ? match.Groups[1].Value : null;
    }
    public async Task<byte[]> GenerateBrandedQrCodeImageAsync(string data, string? logoPath = null, int size = 300, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateQrCodeParameters(data, size);

            // Generate basic QR code first
            var basicQrCode = await GenerateQrCodeImageAsync(data, size, cancellationToken);

            // If no logo, return basic QR code
            if (string.IsNullOrEmpty(logoPath) || !File.Exists(logoPath))
            {
                return basicQrCode;
            }

            // Add logo to QR code
            return await AddLogoToQrCodeAsync(basicQrCode, logoPath, size, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate branded QR code");
            // Fallback to basic QR code
            return await GenerateQrCodeImageAsync(data, size, cancellationToken);
        }
    }

    private async Task<byte[]> AddLogoToQrCodeAsync(byte[] qrCodeBytes, string logoPath, int qrSize, CancellationToken cancellationToken)
    {
        using var qrImage = Image.Load(qrCodeBytes);
        using var logoImage = await Image.LoadAsync(logoPath, cancellationToken);

        // Calculate logo size (10% of QR code size)
        var logoSize = (int)(qrSize * 0.1);
        logoImage.Mutate(x => x.Resize(logoSize, logoSize));

        // Calculate center position
        var x = (qrImage.Width - logoSize) / 2;
        var y = (qrImage.Height - logoSize) / 2;

        // Add white background behind logo for better visibility
        qrImage.Mutate(ctx => 
        {
            // Add white circle background
            // TODO: Fix ImageSharp API usage based on current version
            // ctx.Fill(Color.White, new EllipseGeometry(new PointF(x + logoSize/2, y + logoSize/2), logoSize/2 + 5));
            // Placeholder implementation until proper ImageSharp API is implemented
            // Add logo
            ctx.DrawImage(logoImage, new Point(x, y), 1f);
        });

        using var outputStream = new MemoryStream();
        await qrImage.SaveAsync(outputStream, new PngEncoder(), cancellationToken);
        return outputStream.ToArray();
    }

    private QrCodeValidationResult ParseQrCodeData(string qrData)
    {
        // Try invitation QR pattern
        var invitationMatch = InvitationQrPattern.Match(qrData);
        if (invitationMatch.Success)
        {
            var invitationNumber = invitationMatch.Groups[1].Value;
            var visitorId = int.Parse(invitationMatch.Groups[2].Value);
            return QrCodeValidationResult.Success(invitationNumber, visitorId, QrCodeType.Invitation);
        }

        // Try visitor check-in QR pattern
        var checkInMatch = VisitorCheckInPattern.Match(qrData);
        if (checkInMatch.Success)
        {
            var visitorId = int.Parse(checkInMatch.Groups[1].Value);
            var invitationId = int.Parse(checkInMatch.Groups[2].Value);
            // For check-in QR codes, we'll need to lookup the invitation number
            return QrCodeValidationResult.Success("", visitorId, QrCodeType.VisitorCheckIn);
        }

        return QrCodeValidationResult.Failure("Invalid QR code format");
    }

    private static void ValidateQrCodeParameters(string data, int size)
    {
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("QR code data cannot be null or empty", nameof(data));

        if (size < 50 || size > 2000)
            throw new ArgumentException("QR code size must be between 50 and 2000 pixels", nameof(size));

        if (data.Length > 2953) // QR Code capacity limit for alphanumeric data
            throw new ArgumentException("QR code data is too long", nameof(data));
    }
}
