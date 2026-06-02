using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Application.Speech.DTOs;
using VisitorManagementSystem.Api.Application.Speech.Interfaces;

namespace VisitorManagementSystem.Api.Application.Speech.Commands;

public class TranscribeSpeechCommandHandler : IRequestHandler<TranscribeSpeechCommand, SpeechTranscriptionResultDto>
{
    private readonly ISpeechRecognitionService _recognitionService;
    private readonly ISpeechNormalizationService _normalizationService;
    private readonly IDynamicConfigurationService _config;
    private readonly ILogger<TranscribeSpeechCommandHandler> _logger;

    public TranscribeSpeechCommandHandler(
        ISpeechRecognitionService recognitionService,
        ISpeechNormalizationService normalizationService,
        IDynamicConfigurationService config,
        ILogger<TranscribeSpeechCommandHandler> logger)
    {
        _recognitionService = recognitionService;
        _normalizationService = normalizationService;
        _config = config;
        _logger = logger;
    }

    public async Task<SpeechTranscriptionResultDto> Handle(TranscribeSpeechCommand request, CancellationToken cancellationToken)
    {
        var enabled = await _config.GetConfigurationAsync<bool>("SpeechRecognition", "Enabled", false);
        if (!enabled)
            throw new InvalidOperationException("Speech recognition is disabled.");

        _logger.LogDebug("Transcribing field={Field} language={Language} bytes={Bytes}",
            request.FieldName, request.Language ?? "auto", request.AudioData.Length);

        var raw = await _recognitionService.TranscribeAsync(
            request.AudioData, request.ContentType, request.Language,
            GetInitialPrompt(request.NormalizationType), cancellationToken);

        if (string.IsNullOrWhiteSpace(raw.Text))
        {
            return new SpeechTranscriptionResultDto
            {
                RawText = string.Empty,
                NormalizedValue = string.Empty,
                DetectedLanguage = raw.Language,
                Confidence = raw.LanguageProbability,
                ProcessingTimeMs = raw.ProcessingTimeMs,
                IsEmpty = true,
                Warnings = ["No speech detected. Please try again."],
            };
        }

        var effectiveLanguage = request.Language ?? raw.Language ?? "ar";
        var normalized = await _normalizationService.NormalizeAsync(
            raw.Text, request.NormalizationType, effectiveLanguage, cancellationToken);

        _logger.LogInformation("Transcription complete field={Field} lang={Lang} ms={Ms} raw='{Raw}' normalized='{Normalized}'",
            request.FieldName, raw.Language, raw.ProcessingTimeMs, raw.Text, normalized);

        return new SpeechTranscriptionResultDto
        {
            RawText = raw.Text,
            NormalizedValue = normalized,
            DetectedLanguage = raw.Language,
            Confidence = raw.LanguageProbability,
            ProcessingTimeMs = raw.ProcessingTimeMs,
            IsEmpty = false,
        };
    }

    // Per-field initial prompts nudge Whisper toward the expected vocabulary.
    private static string? GetInitialPrompt(Domain.Enums.SpeechNormalizationType type) => type switch
    {
        Domain.Enums.SpeechNormalizationType.PhoneNumber =>
            // Primes the model toward digit vocabulary; lists both MSA and Lebanese dialect forms
            "رقم الهاتف: صفر واحد تنين تلاتة أربعة خمسة ستة سبعة تمانية تمانة تسعة",
        Domain.Enums.SpeechNormalizationType.PersonName =>
            "الاسم الأول والاسم الأخير",
        Domain.Enums.SpeechNormalizationType.Nationality =>
            "الجنسية: لبناني سوري مصري سعودي كويتي أردني",
        Domain.Enums.SpeechNormalizationType.CompanyName =>
            "اسم الشركة أو المؤسسة",
        Domain.Enums.SpeechNormalizationType.FreeText =>
            "الاسم الشركة الجنسية رقم الهاتف الزيارة",
        _ => null
    };
}
