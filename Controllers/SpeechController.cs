using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Application.Speech.Commands;
using VisitorManagementSystem.Api.Application.Speech.Interfaces;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Offline Arabic/English speech-to-text transcription for visitor registration fields.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SpeechController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ISpeechRecognitionService _recognitionService;
    private readonly IDynamicConfigurationService _config;
    private readonly ILogger<SpeechController> _logger;

    public SpeechController(
        IMediator mediator,
        ISpeechRecognitionService recognitionService,
        IDynamicConfigurationService config,
        ILogger<SpeechController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _recognitionService = recognitionService ?? throw new ArgumentNullException(nameof(recognitionService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Transcribes a short audio clip and returns the normalized text for the specified field.
    /// </summary>
    [HttpPost("transcribe")]
    [RequestSizeLimit(6_000_000)] // 6 MB hard limit (model max is 5 MB)
    public async Task<IActionResult> Transcribe(
        IFormFile audio,
        [FromForm] string fieldName,
        [FromForm] string? language = "ar",
        [FromForm] SpeechNormalizationType normalizationType = SpeechNormalizationType.FreeText,
        CancellationToken cancellationToken = default)
    {
        if (audio == null || audio.Length == 0)
            return BadRequest(BuildError("Audio file is required."));

        byte[] audioData;
        using (var ms = new MemoryStream())
        {
            await audio.CopyToAsync(ms, cancellationToken);
            audioData = ms.ToArray();
        }

        var command = new TranscribeSpeechCommand
        {
            AudioData = audioData,
            ContentType = audio.ContentType ?? "audio/webm",
            FieldName = fieldName,
            Language = string.IsNullOrEmpty(language) ? null : language,
            NormalizationType = normalizationType,
        };

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return SuccessResponse(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Speech transcription failed for field={Field}", fieldName);
            return StatusCode(503, BuildError(ex.Message));
        }
    }

    /// <summary>
    /// Returns the health status of the local speech service.
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var isHealthy = await _recognitionService.IsHealthyAsync(cancellationToken);
        return Ok(new { status = isHealthy ? "ok" : "unavailable" });
    }

    /// <summary>
    /// Returns the current speech recognition settings (non-sensitive) for the frontend.
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        var settings = await _config.GetCategoryConfigurationAsync("SpeechRecognition");

        // Remove any sensitive keys before sending to frontend
        var safeKeys = new[] { "Enabled", "Language", "MaxRecordingSeconds", "AutoStopOnSilence",
                               "AutoMoveNext", "RequireConfirmation", "AllowedFields", "NormalizationEnabled" };

        var filtered = settings
            .Where(kv => safeKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return SuccessResponse(filtered);
    }

    private object BuildError(string message) =>
        new { success = false, message, errors = new[] { message }, timestamp = DateTime.UtcNow };
}
