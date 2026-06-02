using MediatR;
using VisitorManagementSystem.Api.Application.Speech.DTOs;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Speech.Commands;

public record TranscribeSpeechCommand : IRequest<SpeechTranscriptionResultDto>
{
    public byte[] AudioData { get; init; } = [];
    public string ContentType { get; init; } = "audio/webm";
    public string FieldName { get; init; } = string.Empty;
    public string? Language { get; init; }
    public SpeechNormalizationType NormalizationType { get; init; } = SpeechNormalizationType.FreeText;
}
