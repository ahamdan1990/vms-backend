namespace VisitorManagementSystem.Api.Infrastructure.Speech.Options;

public class SpeechRecognitionOptions
{
    public const string SectionName = "SpeechRecognition";

    public string ServiceUrl { get; set; } = "http://127.0.0.1:7100";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxAudioSizeBytes { get; set; } = 5_242_880;
    public bool Enabled { get; set; } = false;
    public string DefaultModel { get; set; } = "small";
    public int ConcurrencyLimit { get; set; } = 2;
}
