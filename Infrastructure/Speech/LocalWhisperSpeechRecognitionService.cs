using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VisitorManagementSystem.Api.Application.Speech.DTOs;
using VisitorManagementSystem.Api.Application.Speech.Interfaces;
using VisitorManagementSystem.Api.Infrastructure.Speech.Options;

namespace VisitorManagementSystem.Api.Infrastructure.Speech;

public class LocalWhisperSpeechRecognitionService : ISpeechRecognitionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SpeechRecognitionOptions _options;
    private readonly ILogger<LocalWhisperSpeechRecognitionService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public LocalWhisperSpeechRecognitionService(
        IHttpClientFactory httpClientFactory,
        IOptions<SpeechRecognitionOptions> options,
        ILogger<LocalWhisperSpeechRecognitionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RawTranscriptionResult> TranscribeAsync(byte[] audioData, string contentType, string? language, string? initialPrompt = null, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("SpeechService");

        using var form = new MultipartFormDataContent();

        var audioContent = new ByteArrayContent(audioData);
        audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(audioContent, "audio", "recording.webm");

        if (!string.IsNullOrEmpty(language))
            form.Add(new StringContent(language), "language");

        if (!string.IsNullOrEmpty(initialPrompt))
            form.Add(new StringContent(initialPrompt), "initial_prompt");

        _logger.LogDebug("Sending {Bytes} bytes to speech service (language={Language})", audioData.Length, language ?? "auto");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync("/transcribe", form, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Speech service is unavailable at {Url}", _options.ServiceUrl);
            throw new InvalidOperationException("Speech service is unavailable. Please try again or type manually.", ex);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Speech service timed out after {Seconds}s", _options.TimeoutSeconds);
            throw new InvalidOperationException("Speech service timed out. Please try again.", new TimeoutException());
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Speech service returned {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Speech service error ({(int)response.StatusCode}).");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<PythonTranscribeResponse>(json, _jsonOptions)
                     ?? throw new InvalidOperationException("Speech service returned an empty response.");

        return new RawTranscriptionResult
        {
            Text = result.Text ?? string.Empty,
            Language = result.Language ?? string.Empty,
            LanguageProbability = result.LanguageProbability,
            ProcessingTimeMs = result.ProcessingTimeMs,
        };
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("SpeechService");
            var response = await client.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private sealed class PythonTranscribeResponse
    {
        public string? Text { get; set; }
        public string? Language { get; set; }
        public double LanguageProbability { get; set; }
        public long ProcessingTimeMs { get; set; }
    }
}
