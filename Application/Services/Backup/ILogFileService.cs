using VisitorManagementSystem.Api.Application.DTOs.Backup;

namespace VisitorManagementSystem.Api.Application.Services.Backup;

public interface ILogFileService
{
    /// <summary>Returns size, file list, and alert level for the backend log folder.</summary>
    Task<LogFolderHealthDto> GetLogFolderHealthAsync(CancellationToken ct = default);

    /// <summary>Deletes log files whose last-write time is older than retentionDays.</summary>
    Task<LogPurgeResultDto> PurgeOldLogsAsync(int retentionDays, CancellationToken ct = default);

    Task<LogSettingsDto> GetLogSettingsAsync(CancellationToken ct = default);
    Task<(bool Success, string? Error)> UpdateLogSettingsAsync(UpdateLogSettingsRequest request, CancellationToken ct = default);

    string GetLogFolderPath();
}
