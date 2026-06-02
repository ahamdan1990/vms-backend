using VisitorManagementSystem.Api.Application.Speech.DTOs;

namespace VisitorManagementSystem.Api.Application.Speech.Interfaces;

public interface ISpeechRecognitionService
{
    Task<RawTranscriptionResult> TranscribeAsync(byte[] audioData, string contentType, string? language, string? initialPrompt = null, CancellationToken ct = default);
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}
