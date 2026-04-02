namespace VisitorManagementSystem.Api.Application.Services.Backup;

/// <summary>
/// Reads and writes the deduplication state for storage threshold alerts.
/// State is persisted in SystemConfiguration so it survives application restarts.
/// All reads bypass the DynamicConfigurationService cache to ensure freshness.
/// </summary>
public interface IStorageAlertStateStore
{
    Task<string> GetLastDbAlertLevelAsync(CancellationToken ct = default);
    Task SetLastDbAlertLevelAsync(string level, CancellationToken ct = default);

    Task<string> GetLastDiskAlertLevelAsync(CancellationToken ct = default);
    Task SetLastDiskAlertLevelAsync(string level, CancellationToken ct = default);

    Task<DateTime?> GetLastAlertFiredAtAsync(CancellationToken ct = default);
    Task SetLastAlertFiredAtAsync(DateTime utcNow, CancellationToken ct = default);

    Task<DateTime?> GetLastEventBackupAtAsync(CancellationToken ct = default);
    Task SetLastEventBackupAtAsync(DateTime utcNow, CancellationToken ct = default);
}
