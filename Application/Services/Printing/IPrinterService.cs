namespace VisitorManagementSystem.Api.Application.Services.Printing;

/// <summary>
/// Sends a print job directly from the backend server to a configured network printer.
/// Used as the first tier in the three-tier print fallback:
///   1. Backend → printer (this service)
///   2. Bridge  → printer (frontend calls localhost:7891/print-badge)
///   3. Browser → window.print() (frontend opens PDF in new window)
/// </summary>
public interface IPrinterService
{
    /// <summary>
    /// Returns true if a printer is currently configured and reachable.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an A4 PDF badge to the configured IPP printer.
    /// </summary>
    Task<PrintJobResult> PrintPdfAsync(
        byte[] pdfBytes,
        string jobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a ZPL label string to the configured ZPL (label) printer.
    /// </summary>
    Task<PrintJobResult> PrintZplAsync(
        string zplContent,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a backend print job attempt.
/// </summary>
public record PrintJobResult(
    bool Success,
    string? ErrorMessage = null,
    string? Protocol = null)
{
    public static PrintJobResult Ok(string protocol) => new(true, Protocol: protocol);
    public static PrintJobResult Fail(string error) => new(false, ErrorMessage: error);
}
