using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Services.Badge;

/// <summary>
/// Generates visitor badge content in various output formats.
/// </summary>
public interface IBadgeService
{
    /// <summary>
    /// Generates an A4 PDF visitor badge suitable for printing on any office printer.
    /// The badge includes visitor photo, name, company, host info, date/time, location,
    /// QR code (for fast re-check-out), and a VIP indicator when applicable.
    /// </summary>
    Task<byte[]> GenerateBadgePdfAsync(
        Invitation invitation,
        byte[]? visitorPhotoBytes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a ZPL (Zebra Programming Language) string for label printers.
    /// Designed for a 4"×3" label at 203 DPI, common for Zebra GK420d / GC420t.
    /// </summary>
    string GenerateBadgeZpl(Invitation invitation);
}
