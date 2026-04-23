namespace VisitorManagementSystem.Api.Application.Services.Printing;

/// <summary>
/// No-op printer service registered when no printer is configured in SystemConfigurations.
/// Returns success=false with a helpful message so the frontend falls through to the bridge
/// or browser print fallback.
/// </summary>
public sealed class NullPrinterService : IPrinterService
{
    private const string NoConfigMessage =
        "No server-side printer is configured. The browser will use the local bridge or browser print fallback.";

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<PrintJobResult> PrintPdfAsync(byte[] pdfBytes, string jobName, CancellationToken cancellationToken = default) =>
        Task.FromResult(PrintJobResult.Fail(NoConfigMessage));

    public Task<PrintJobResult> PrintZplAsync(string zplContent, CancellationToken cancellationToken = default) =>
        Task.FromResult(PrintJobResult.Fail(NoConfigMessage));
}
