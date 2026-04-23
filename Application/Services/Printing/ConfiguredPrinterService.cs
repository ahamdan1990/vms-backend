using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Printing;

/// <summary>
/// Reads hardware printer configuration from SystemConfigurations at runtime
/// (Category = "Hardware", Keys = "Printer:Protocol", "Printer:Address", "Printer:Port")
/// and delegates to the appropriate IPP or ZPL implementation.
///
/// Using null/not-found config gracefully falls through to NullPrinterService, allowing
/// the frontend to use the bridge or browser fallback without any server errors.
///
/// SystemConfiguration keys:
///   Hardware / Printer:Protocol  → "ipp" | "zpl"  (required)
///   Hardware / Printer:Address   → IPP URI (http://...) or ZPL hostname
///   Hardware / Printer:Port      → TCP port for ZPL (default 9100)
/// </summary>
public sealed class ConfiguredPrinterService : IPrinterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerFactory _loggerFactory;

    public ConfiguredPrinterService(IUnitOfWork unitOfWork, ILoggerFactory loggerFactory)
    {
        _unitOfWork    = unitOfWork;
        _loggerFactory = loggerFactory;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var service = await ResolveAsync(cancellationToken);
        return await service.IsAvailableAsync(cancellationToken);
    }

    public async Task<PrintJobResult> PrintPdfAsync(byte[] pdfBytes, string jobName, CancellationToken cancellationToken = default)
    {
        var service = await ResolveAsync(cancellationToken);
        return await service.PrintPdfAsync(pdfBytes, jobName, cancellationToken);
    }

    public async Task<PrintJobResult> PrintZplAsync(string zplContent, CancellationToken cancellationToken = default)
    {
        var service = await ResolveAsync(cancellationToken);
        return await service.PrintZplAsync(zplContent, cancellationToken);
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    private async Task<IPrinterService> ResolveAsync(CancellationToken ct)
    {
        try
        {
            var protocol = await GetConfigAsync("Printer:Protocol", ct);
            var address  = await GetConfigAsync("Printer:Address",  ct);

            if (string.IsNullOrWhiteSpace(protocol) || string.IsNullOrWhiteSpace(address))
                return new NullPrinterService();

            if (string.Equals(protocol, "ipp", StringComparison.OrdinalIgnoreCase))
            {
                return new IppPrinterService(address,
                    _loggerFactory.CreateLogger<IppPrinterService>());
            }

            if (string.Equals(protocol, "zpl", StringComparison.OrdinalIgnoreCase))
            {
                var portStr = await GetConfigAsync("Printer:Port", ct);
                int port    = int.TryParse(portStr, out var p) ? p : 9100;

                return new ZplPrinterService(address, port,
                    _loggerFactory.CreateLogger<ZplPrinterService>());
            }

            return new NullPrinterService();
        }
        catch
        {
            // DB not ready or no config table — fall through gracefully
            return new NullPrinterService();
        }
    }

    private async Task<string?> GetConfigAsync(string key, CancellationToken ct)
    {
        var cfg = await _unitOfWork.SystemConfigurations
            .GetByCategoryAndKeyAsync("Hardware", key, ct);
        return cfg?.Value;
    }
}
