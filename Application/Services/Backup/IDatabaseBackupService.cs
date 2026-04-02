using VisitorManagementSystem.Api.Application.DTOs.Backup;

namespace VisitorManagementSystem.Api.Application.Services.Backup;

public record BackupResult(bool Success, string? FilePath, string? FileName, long FileSizeBytes, string? ErrorMessage);

/// <summary>
/// Executes SQL Server BACKUP DATABASE commands, manages backup file naming,
/// enforces concurrency (one backup at a time), runs retention cleanup, and
/// records each operation in the BackupRecords table.
/// </summary>
public interface IDatabaseBackupService
{
    /// <summary>
    /// Returns true if a backup is currently running.
    /// </summary>
    bool IsBackupRunning { get; }

    /// <summary>
    /// Executes a full database backup.
    /// Thread-safe: rejects concurrent calls with a specific error.
    /// Performs pre-flight disk space check before starting.
    /// Runs retention cleanup on success.
    /// </summary>
    Task<BackupResult> ExecuteBackupAsync(
        string triggerType,
        int? triggeredByUserId,
        string triggeredByDisplay,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the last N backup records for display in the history table.
    /// </summary>
    Task<List<BackupRecordDto>> GetHistoryAsync(int count = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recent completed backup record, or null.
    /// </summary>
    Task<BackupRecordDto?> GetLastSuccessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recent failed backup record, or null.
    /// </summary>
    Task<BackupRecordDto?> GetLastFailureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads current backup settings from SystemConfiguration.
    /// Always fresh (no cache).
    /// </summary>
    Task<BackupSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists backup settings to SystemConfiguration and recalculates NextRunAt.
    /// Validates destination path before saving.
    /// </summary>
    Task<(bool Success, string? Error)> UpdateSettingsAsync(
        UpdateBackupSettingsRequest request,
        CancellationToken cancellationToken = default);
}
