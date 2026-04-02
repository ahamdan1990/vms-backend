using VisitorManagementSystem.Api.Application.DTOs.Backup;

namespace VisitorManagementSystem.Api.Application.Services.Backup;

public interface IDataPurgeService
{
    /// <summary>Dry-run: count rows that would be deleted given current settings.</summary>
    Task<PurgePreviewDto> GetPurgePreviewAsync(RetentionSettingsDto settings, CancellationToken ct = default);

    /// <summary>Execute the purge. Returns counts of what was actually deleted.</summary>
    Task<PurgeResultDto> ExecutePurgeAsync(RetentionSettingsDto settings, CancellationToken ct = default);

    Task<RetentionSettingsDto> GetRetentionSettingsAsync(CancellationToken ct = default);
    Task<(bool Success, string? Error)> UpdateRetentionSettingsAsync(UpdateRetentionSettingsRequest request, CancellationToken ct = default);
}
