namespace VisitorManagementSystem.Api.Application.Speech.DTOs;

public class SpeechTranscriptionResultDto
{
    public string RawText { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public string DetectedLanguage { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    public long ProcessingTimeMs { get; set; }
    public bool IsEmpty { get; set; }
    public List<string> Warnings { get; set; } = [];
}
