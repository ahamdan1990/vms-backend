namespace VisitorManagementSystem.Api.Application.Speech.DTOs;

/// <summary>
/// All parameters needed for a single transcription call, built by the handler from the DB settings.
/// </summary>
public record SpeechTranscribeRequest(
    byte[] Audio,
    string ContentType,
    string Language,            // "ar", "en", "auto" — "auto" omits the language field sent to Whisper
    string Model,               // "tiny", "base", "small", "medium", "large-v3"
    string? InitialPrompt,
    double ConfidenceThreshold, // minimum LanguageProbability to accept the result
    int MaxAudioSizeBytes);
