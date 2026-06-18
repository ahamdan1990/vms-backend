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

        // Read all settings from DB (IDynamicConfigurationService caches for 30 min)
        var dbLanguage    = await _config.GetConfigurationAsync<string>("SpeechRecognition", "Language", "ar");
        var model         = await _config.GetConfigurationAsync<string>("SpeechRecognition", "Model", "small");
        var confidence    = await _config.GetConfigurationAsync<double>("SpeechRecognition", "ConfidenceThreshold", 0.5);
        var maxBytes      = await _config.GetConfigurationAsync<int>("SpeechRecognition", "MaxAudioSizeBytes", 5_242_880);
        var normalizeEnabled = await _config.GetConfigurationAsync<bool>("SpeechRecognition", "NormalizationEnabled", true);

        // Request language overrides the DB default when explicitly provided
        var effectiveLanguage = !string.IsNullOrEmpty(request.Language) ? request.Language : (dbLanguage ?? "ar");

        _logger.LogDebug("Transcribing field={Field} language={Language} model={Model} bytes={Bytes}",
            request.FieldName, effectiveLanguage, model, request.AudioData.Length);

        var transcribeRequest = new SpeechTranscribeRequest(
            Audio: request.AudioData,
            ContentType: request.ContentType,
            Language: effectiveLanguage,
            Model: model ?? "small",
            InitialPrompt: GetInitialPrompt(request.NormalizationType, effectiveLanguage),
            ConfidenceThreshold: confidence,
            MaxAudioSizeBytes: maxBytes);

        var raw = await _recognitionService.TranscribeAsync(transcribeRequest, cancellationToken);

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

        // Flag low-confidence results rather than silently accepting them
        var warnings = new List<string>();
        if (raw.LanguageProbability > 0 && raw.LanguageProbability < confidence)
        {
            warnings.Add($"Low language detection confidence ({raw.LanguageProbability:P0}). Please review the result.");
            _logger.LogDebug("Low confidence result: field={Field} confidence={Confidence:F2} threshold={Threshold:F2}",
                request.FieldName, raw.LanguageProbability, confidence);
        }

        var detectedLang = !string.IsNullOrEmpty(raw.Language) ? raw.Language : effectiveLanguage;
        var normalized = normalizeEnabled
            ? await _normalizationService.NormalizeAsync(raw.Text, request.NormalizationType, detectedLang, cancellationToken)
            : raw.Text;

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
            Warnings = warnings,
        };
    }

    // Per-field initial prompts nudge Whisper toward the expected vocabulary.
    // Arabic-only — skip for other languages to avoid biasing the model.
    private static string? GetInitialPrompt(Domain.Enums.SpeechNormalizationType type, string language)
    {
        if (!language.Equals("ar", StringComparison.OrdinalIgnoreCase))
            return null;

        return type switch
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
}
