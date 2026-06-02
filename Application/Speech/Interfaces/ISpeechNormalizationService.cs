using VisitorManagementSystem.Api.Application.Speech.DTOs;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Speech.Interfaces;

public interface ISpeechNormalizationService
{
    Task<string> NormalizeAsync(string rawText, SpeechNormalizationType type, string language, CancellationToken ct = default);
}
