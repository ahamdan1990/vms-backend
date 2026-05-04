using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Services.CivilDefense;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers.CivilDefense;

[Authorize]
[ApiController]
[Route("api/civil-defense/ocr")]
public class CdOcrController : BaseController
{
    private readonly ICdOcrService _ocrService;

    public CdOcrController(ICdOcrService ocrService)
    {
        _ocrService = ocrService;
    }

    [HttpPost("extract")]
    [Authorize(Policy = Permissions.CivilDefense.ScanDocument)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> ExtractDocumentFields(IFormFile document, CancellationToken ct)
    {
        if (document == null || document.Length == 0)
            return BadRequestResponse("No document image provided.");

        using var ms = new MemoryStream();
        await document.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var result = await _ocrService.ExtractFromImageAsync(bytes, ct);

        if (!result.Success)
            return BadRequestResponse(result.ErrorMessage ?? "OCR extraction failed.");

        return SuccessResponse(result, "Document fields extracted.");
    }

    [HttpGet("status")]
    [Authorize(Policy = Permissions.CivilDefense.ScanDocument)]
    public IActionResult GetOcrStatus()
    {
        return SuccessResponse(new { available = _ocrService.IsAvailable });
    }
}
