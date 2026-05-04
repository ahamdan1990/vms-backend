using System.Text.RegularExpressions;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

namespace VisitorManagementSystem.Api.Application.Services.CivilDefense;

/// <summary>
/// Offline OCR for Arabic ID/passport documents using Tesseract.NET.
/// Requires ara.traineddata in the tessdata directory configured via
/// appsettings: "Tesseract:TessDataPath" and "Tesseract:Languages" (default "ara+eng").
/// </summary>
public class CdOcrService : ICdOcrService
{
    private readonly ILogger<CdOcrService> _logger;
    private readonly string _tessDataPath;
    private readonly string _languages;
    private readonly bool _available;

    // Patterns for Arabic national ID fields
    private static readonly Regex NationalIdPattern =
        new(@"\b\d{10,14}\b", RegexOptions.Compiled);
    private static readonly Regex DatePattern =
        new(@"\b(\d{2}[\/\-]\d{2}[\/\-]\d{4}|\d{4}[\/\-]\d{2}[\/\-]\d{2})\b", RegexOptions.Compiled);

    public bool IsAvailable => _available;

    public CdOcrService(IConfiguration configuration, ILogger<CdOcrService> logger)
    {
        _logger = logger;
        _tessDataPath = configuration["Tesseract:TessDataPath"] ?? Path.Combine(AppContext.BaseDirectory, "tessdata");
        _languages    = configuration["Tesseract:Languages"] ?? "ara+eng";
        _available    = Directory.Exists(_tessDataPath);

        if (!_available)
            _logger.LogWarning("Tesseract tessdata not found at '{Path}'. OCR will be unavailable.", _tessDataPath);
    }

    public async Task<CdOcrResultDto> ExtractFromImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        if (!_available)
            return new CdOcrResultDto { Success = false, ErrorMessage = "OCR engine not configured." };

        try
        {
            // Tesseract.NET is loaded dynamically to avoid a hard compile dependency.
            // If Tesseract NuGet is not installed this method returns a clear error message.
            return await Task.Run(() => RunTesseract(imageBytes), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR extraction failed");
            return new CdOcrResultDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    private CdOcrResultDto RunTesseract(byte[] imageBytes)
    {
        // Reflective invocation so the project compiles without Tesseract.NET referenced.
        // Add the NuGet package "Tesseract" to enable OCR at runtime.
        var tesseractAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Tesseract");

        if (tesseractAssembly == null)
        {
            _logger.LogWarning("Tesseract NuGet package is not loaded. Install 'Tesseract' package.");
            return new CdOcrResultDto { Success = false, ErrorMessage = "Tesseract package not installed." };
        }

        var engineType = tesseractAssembly.GetType("Tesseract.TesseractEngine")!;
        var pixType    = tesseractAssembly.GetType("Tesseract.Pix")!;

        using var engine = (IDisposable)Activator.CreateInstance(engineType, _tessDataPath, _languages, 3)!;

        var loadFromMemMethod = pixType.GetMethod("LoadFromMemory")!;
        using var pix = (IDisposable)loadFromMemMethod.Invoke(null, [imageBytes])!;

        var processMethod = engineType.GetMethod("Process", [pixType])!;
        using var page = (IDisposable)processMethod.Invoke(engine, [pix])!;

        var getMeanConfidence = page.GetType().GetMethod("GetMeanConfidence")!;
        var getTextMethod     = page.GetType().GetMethod("GetText")!;

        var confidence = (float)getMeanConfidence.Invoke(page, null)!;
        var rawText    = (string)getTextMethod.Invoke(page, null)!;

        return ParseIdText(rawText.Trim(), confidence / 100.0);
    }

    private static CdOcrResultDto ParseIdText(string rawText, double confidence)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return new CdOcrResultDto { Success = false, ErrorMessage = "No text extracted." };

        var lines   = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var idMatch = NationalIdPattern.Match(rawText);
        var dates   = DatePattern.Matches(rawText);

        return new CdOcrResultDto
        {
            Success      = true,
            RawText      = rawText,
            NationalId   = idMatch.Success ? idMatch.Value : null,
            DateOfBirth  = dates.Count > 0 ? dates[0].Value : null,
            ExpiryDate   = dates.Count > 1 ? dates[1].Value : null,
            DocumentType = rawText.Contains("PASSPORT", StringComparison.OrdinalIgnoreCase) ? "Passport" : "NationalId",
            Confidence   = confidence,
        };
    }
}
