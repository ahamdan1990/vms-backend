using VisitorManagementSystem.Api.Application.DTOs.Backup;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Backup;

/// <summary>
/// Persists storage alert dedup state in SystemConfiguration.
/// Always reads directly from the repository (no cache) to ensure the background
/// service sees the latest values even after an app restart or parallel update.
/// </summary>
public class StorageAlertStateStore : IStorageAlertStateStore
{
    private readonly IUnitOfWork _unitOfWork;

    public StorageAlertStateStore(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GetLastDbAlertLevelAsync(CancellationToken ct = default)
        => await GetStringAsync("Storage", "LastDbAlertLevel", StorageAlertLevel.None, ct);

    public async Task SetLastDbAlertLevelAsync(string level, CancellationToken ct = default)
        => await SetStringAsync("Storage", "LastDbAlertLevel", level, ct);

    public async Task<string> GetLastDiskAlertLevelAsync(CancellationToken ct = default)
        => await GetStringAsync("Storage", "LastDiskAlertLevel", StorageAlertLevel.None, ct);

    public async Task SetLastDiskAlertLevelAsync(string level, CancellationToken ct = default)
        => await SetStringAsync("Storage", "LastDiskAlertLevel", level, ct);

    public async Task<DateTime?> GetLastAlertFiredAtAsync(CancellationToken ct = default)
        => await GetDateTimeAsync("Storage", "LastAlertFiredAt", ct);

    public async Task SetLastAlertFiredAtAsync(DateTime utcNow, CancellationToken ct = default)
        => await SetStringAsync("Storage", "LastAlertFiredAt", utcNow.ToString("O"), ct);

    public async Task<DateTime?> GetLastEventBackupAtAsync(CancellationToken ct = default)
        => await GetDateTimeAsync("Backup", "LastEventBackupAt", ct);

    public async Task SetLastEventBackupAtAsync(DateTime utcNow, CancellationToken ct = default)
        => await SetStringAsync("Backup", "LastEventBackupAt", utcNow.ToString("O"), ct);

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<string> GetStringAsync(string category, string key, string defaultValue, CancellationToken ct)
    {
        var config = await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync(category, key, ct);
        return string.IsNullOrEmpty(config?.Value) ? defaultValue : config.Value;
    }

    private async Task<DateTime?> GetDateTimeAsync(string category, string key, CancellationToken ct)
    {
        var raw = await GetStringAsync(category, key, string.Empty, ct);
        return DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt
            : null;
    }

    private async Task SetStringAsync(string category, string key, string value, CancellationToken ct)
    {
        var config = await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync(category, key, ct);
        if (config == null) return; // Seeded on first deploy; if missing, skip gracefully

        config.Value = value;
        config.UpdateModifiedOn();
        _unitOfWork.SystemConfigurations.Update(config);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
