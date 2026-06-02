using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Seeds SpeechRecognition system configurations (offline Arabic STT via faster-whisper).
/// Called from ComprehensiveConfigurationSeeder.
/// </summary>
public static class SpeechConfigurationSeeder
{
    public static Task SeedAsync(List<SystemConfiguration> configurations, DateTime now, int? createdBy)
    {
        configurations.AddRange(new[]
        {
            Create("SpeechRecognition", "Enabled",
                "true", "bool", "Master switch — enables or disables the speech input feature globally.", now, createdBy),

            Create("SpeechRecognition", "Provider",
                "LocalWhisper", "string", "Speech recognition engine. Currently only 'LocalWhisper' is supported.", now, createdBy),

            Create("SpeechRecognition", "ServiceUrl",
                "http://127.0.0.1:7100", "string", "URL of the local faster-whisper speech service.", now, createdBy),

            Create("SpeechRecognition", "Model",
                "small", "string", "Whisper model name: tiny / base / small / medium / large-v3.", now, createdBy),

            Create("SpeechRecognition", "Language",
                "ar", "string", "Default transcription language (ar = Arabic, en = English, auto = detect).", now, createdBy),

            Create("SpeechRecognition", "MaxRecordingSeconds",
                "5", "int", "Maximum microphone recording duration in seconds.", now, createdBy),

            Create("SpeechRecognition", "AutoStopOnSilence",
                "true", "bool", "Automatically stop recording when silence is detected.", now, createdBy),

            Create("SpeechRecognition", "AutoMoveNext",
                "false", "bool", "Automatically move focus to the next field after successful transcription.", now, createdBy),

            Create("SpeechRecognition", "RequireConfirmation",
                "false", "bool", "Show a confirmation step before filling the field with transcribed text.", now, createdBy),

            Create("SpeechRecognition", "StoreAudio",
                "false", "bool", "Persist raw audio blobs for quality review. Disabled by default for privacy.", now, createdBy),

            Create("SpeechRecognition", "ConfidenceThreshold",
                "0.5", "decimal", "Minimum language-detection confidence (0–1) required to accept a result.", now, createdBy),

            Create("SpeechRecognition", "AllowedFields",
                "firstName,lastName,company,nationality,phoneNumber,phone,jobTitle,purposeOfVisit,affiliatedOrganization", "string",
                "Comma-separated list of visitor form fields where voice input is enabled.", now, createdBy),

            Create("SpeechRecognition", "NormalizationEnabled",
                "true", "bool", "Apply field-specific Arabic normalization (filler words, digits, hamza, etc.).", now, createdBy),

            Create("SpeechRecognition", "MaxAudioSizeBytes",
                "5242880", "int", "Maximum audio upload size in bytes (default 5 MB).", now, createdBy),
        });

        return Task.CompletedTask;
    }

    private static SystemConfiguration Create(
        string category, string key, string value, string dataType, string description,
        DateTime now, int? createdBy) =>
        new()
        {
            Category = category,
            Key = key,
            Value = value,
            DataType = dataType,
            Description = description,
            DefaultValue = value,
            RequiresRestart = false,
            IsEncrypted = false,
            IsSensitive = false,
            IsReadOnly = false,
            Group = "Speech Recognition",
            DisplayOrder = 0,
            Environment = "All",
            CreatedBy = createdBy,
            CreatedOn = now,
            IsActive = true,
        };
}
