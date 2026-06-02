namespace VisitorManagementSystem.Api.Application.Speech.DTOs;

public class RawTranscriptionResult
{
    public string Text { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public double LanguageProbability { get; set; }
    public long ProcessingTimeMs { get; set; }
}
