using SharpIpp;
using SharpIpp.Models.Requests;
using SharpIpp.Protocol.Models;

namespace VisitorManagementSystem.Api.Application.Services.Printing;

/// <summary>
/// IPP printer service using SharpIppNext 3.0.0.
/// Sends PDF directly to an IPP-capable office printer / MFP.
/// </summary>
public sealed class IppPrinterService : IPrinterService
{
    private readonly string _printerUri;
    private readonly ILogger<IppPrinterService> _logger;

    public IppPrinterService(string printerUri, ILogger<IppPrinterService> logger)
    {
        _printerUri = printerUri;
        _logger     = logger;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SharpIppClient();
            var response = await client.GetPrinterAttributesAsync(
                new GetPrinterAttributesRequest
                {
                    OperationAttributes = new GetPrinterAttributesOperationAttributes
                    {
                        PrinterUri          = new Uri(_printerUri),
                        RequestedAttributes = new[] { "printer-state" },
                    },
                }, cancellationToken);

            return response.StatusCode is IppStatusCode.SuccessfulOk
                or IppStatusCode.SuccessfulOkIgnoredOrSubstitutedAttributes;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PrintJobResult> PrintPdfAsync(
        byte[] pdfBytes,
        string jobName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("IPP: printing '{Job}' ({Size:N0} bytes) to {Uri}",
                jobName, pdfBytes.Length, _printerUri);

            using var stream = new MemoryStream(pdfBytes);
            using var client = new SharpIppClient();

            var response = await client.PrintJobAsync(new PrintJobRequest
            {
                Document            = stream,
                OperationAttributes = new PrintJobOperationAttributes
                {
                    PrinterUri           = new Uri(_printerUri),
                    JobName              = jobName,
                    DocumentFormat       = "application/pdf",
                    DocumentName         = $"{jobName}.pdf",
                    IppAttributeFidelity = false,
                },
                JobTemplateAttributes = new JobTemplateAttributes
                {
                    Copies = 1,
                    Sides  = Sides.OneSided,
                    OrientationRequested = Orientation.Portrait,
                },
            }, cancellationToken);

            if (response.StatusCode is IppStatusCode.SuccessfulOk
                or IppStatusCode.SuccessfulOkIgnoredOrSubstitutedAttributes
                or IppStatusCode.SuccessfulOkConflictingAttributes)
            {
                _logger.LogInformation("IPP job accepted. ID: {Id}, State: {State}",
                    response.JobAttributes?.JobId,
                    response.JobAttributes?.JobState);
                return PrintJobResult.Ok("ipp");
            }

            var err = $"IPP error: {response.StatusCode}";
            _logger.LogWarning("IPP job rejected: {Error}", err);
            return PrintJobResult.Fail(err);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IPP print failed for job '{Job}'", jobName);
            return PrintJobResult.Fail($"IPP print failed: {ex.Message}");
        }
    }

    public Task<PrintJobResult> PrintZplAsync(
        string zplContent,
        CancellationToken cancellationToken = default)
        => Task.FromResult(PrintJobResult.Fail(
            "IPP printer does not support ZPL. Use the ZPL printer service instead."));
}
