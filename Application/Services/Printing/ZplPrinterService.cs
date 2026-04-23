using System.Net.Sockets;
using System.Text;

namespace VisitorManagementSystem.Api.Application.Services.Printing;

/// <summary>
/// Printer service for ZPL (Zebra Programming Language) label printers.
/// Sends raw ZPL text directly via TCP to port 9100.
/// Compatible with Zebra, TSC, Cab, Honeywell, and other thermal label printers.
/// </summary>
public sealed class ZplPrinterService : IPrinterService
{
    private const int ConnectTimeoutMs = 5_000;
    private const int WriteTimeoutMs   = 10_000;

    private readonly string _printerHost;
    private readonly int _printerPort;
    private readonly ILogger<ZplPrinterService> _logger;

    public ZplPrinterService(string printerHost, int printerPort, ILogger<ZplPrinterService> logger)
    {
        _printerHost = printerHost;
        _printerPort = printerPort;
        _logger      = logger;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var tcp = new TcpClient();
            var connect = tcp.ConnectAsync(_printerHost, _printerPort, cancellationToken).AsTask();
            return await Task.WhenAny(connect, Task.Delay(3000, cancellationToken)) == connect
                   && !connect.IsFaulted;
        }
        catch
        {
            return false;
        }
    }

    public Task<PrintJobResult> PrintPdfAsync(byte[] pdfBytes, string jobName, CancellationToken cancellationToken = default)
        => Task.FromResult(PrintJobResult.Fail("ZPL label printer cannot accept PDF. Use the IPP printer service or the browser print fallback for PDF badges."));

    public async Task<PrintJobResult> PrintZplAsync(string zplContent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("ZPL: sending {Chars} chars to {Host}:{Port}",
                zplContent.Length, _printerHost, _printerPort);

            using var tcp = new TcpClient();
            var connect = tcp.ConnectAsync(_printerHost, _printerPort, cancellationToken).AsTask();

            if (await Task.WhenAny(connect, Task.Delay(ConnectTimeoutMs, cancellationToken)) != connect)
                return PrintJobResult.Fail($"Timed out connecting to ZPL printer at {_printerHost}:{_printerPort}.");

            await connect;

            using var stream = tcp.GetStream();
            stream.WriteTimeout = WriteTimeoutMs;

            byte[] data = Encoding.UTF8.GetBytes(zplContent);
            await stream.WriteAsync(data, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            _logger.LogInformation("ZPL job sent ({Bytes} bytes)", data.Length);
            return PrintJobResult.Ok("zpl");
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Socket error sending ZPL to {Host}:{Port}", _printerHost, _printerPort);
            return PrintJobResult.Fail(
                $"Cannot reach ZPL printer at {_printerHost}:{_printerPort}. Verify the printer is on and connected.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZPL print failed");
            return PrintJobResult.Fail($"ZPL print failed: {ex.Message}");
        }
    }
}
