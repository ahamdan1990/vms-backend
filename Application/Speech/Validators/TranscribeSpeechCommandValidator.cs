using FluentValidation;
using VisitorManagementSystem.Api.Application.Speech.Commands;

namespace VisitorManagementSystem.Api.Application.Speech.Validators;

public class TranscribeSpeechCommandValidator : AbstractValidator<TranscribeSpeechCommand>
{
    private const int MaxAudioBytes = 5_242_880; // 5 MB
    private static readonly string[] AllowedLanguages = ["ar", "en"];

    public TranscribeSpeechCommandValidator()
    {
        RuleFor(x => x.AudioData)
            .NotEmpty().WithMessage("Audio data is required.")
            .Must(d => d.Length <= MaxAudioBytes)
            .WithMessage($"Audio file must not exceed {MaxAudioBytes / 1024 / 1024} MB.");

        RuleFor(x => x.FieldName)
            .NotEmpty().WithMessage("Field name is required.");

        RuleFor(x => x.Language)
            .Must(l => l == null || AllowedLanguages.Contains(l))
            .WithMessage("Language must be 'ar', 'en', or omitted for auto-detect.");
    }
}
