using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

namespace VisitorManagementSystem.Api.Application.Services.CivilDefense;

public interface ICdOcrService
{
    Task<CdOcrResultDto> ExtractFromImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default);
    bool IsAvailable { get; }
}
