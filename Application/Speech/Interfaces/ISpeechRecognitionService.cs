using VisitorManagementSystem.Api.Application.Speech.DTOs;

namespace VisitorManagementSystem.Api.Application.Speech.Interfaces;

public interface ISpeechRecognitionService
{
    Task<RawTranscriptionResult> TranscribeAsync(SpeechTranscribeRequest request, CancellationToken ct = default);
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}
