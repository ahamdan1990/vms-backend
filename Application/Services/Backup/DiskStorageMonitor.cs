using VisitorManagementSystem.Api.Application.DTOs.Backup;

namespace VisitorManagementSystem.Api.Application.Services.Backup;

public class DiskStorageMonitor : IDiskStorageMonitor
{
    private readonly ILogger<DiskStorageMonitor> _logger;

    public DiskStorageMonitor(ILogger<DiskStorageMonitor> logger)
    {
        _logger = logger;
    }

    public Task<List<DriveHealthDto>> GetDriveHealthAsync(
        IEnumerable<string> dbFilePaths,
        string backupDestinationPath,
        int warnFreePercent,
        int criticalFreePercent,
        CancellationToken cancellationToken = default)
    {
        var result = new List<DriveHealthDto>();

        // Collect all unique drive roots: one per DB file path + one for backup destination
        var driveEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in dbFilePaths)
        {
            var root = GetDriveRoot(path);
            if (root != null)
                driveEntries.TryAdd(root, "Database");
        }

        if (!string.IsNullOrWhiteSpace(backupDestinationPath))
        {
            var backupRoot = GetDriveRoot(backupDestinationPath);
            if (backupRoot != null)
            {
                if (driveEntries.ContainsKey(backupRoot))
                    driveEntries[backupRoot] = "Database & Backups"; // Same drive
                else
                    driveEntries[backupRoot] = "Backups";
            }
        }

        foreach (var (root, purpose) in driveEntries)
        {
            try
            {
                var drive = new DriveInfo(root);
                if (!drive.IsReady)
                {
                    result.Add(new DriveHealthDto(root, purpose, 0, 0, 0, false, StorageAlertLevel.Critical));
                    continue;
                }

                var freeBytes = drive.AvailableFreeSpace;
                var totalBytes = drive.TotalSize;
                var freePercent = totalBytes > 0 ? Math.Round((double)freeBytes / totalBytes * 100, 1) : 0;

                bool isWritable = purpose.Contains("Backups") && !string.IsNullOrWhiteSpace(backupDestinationPath)
                    ? TestWritable(backupDestinationPath)
                    : true; // DB drive writability not checked here — SQL Server owns that

                string alertLevel = DetermineAlertLevel(freePercent, warnFreePercent, criticalFreePercent);

                result.Add(new DriveHealthDto(root, purpose, totalBytes, freeBytes, freePercent, isWritable, alertLevel));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query drive info for {Root}", root);
                result.Add(new DriveHealthDto(root, purpose, 0, 0, 0, false, StorageAlertLevel.Critical));
            }
        }

        return Task.FromResult(result);
    }

    public async Task<(bool IsWritable, string? Error)> TestPathWritableAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return (false, "Backup destination path is not configured.");

            if (!Path.IsPathRooted(path))
                return (false, "Backup destination must be an absolute path.");

            if (path.Contains(".."))
                return (false, "Backup destination path must not contain '..' segments.");

            if (!Directory.Exists(path))
            {
                try { Directory.CreateDirectory(path); }
                catch (Exception ex) { return (false, $"Cannot create directory: {ex.Message}"); }
            }

            var testFile = Path.Combine(path, $".vms_write_test_{Guid.NewGuid():N}");
            await File.WriteAllTextAsync(testFile, "test", cancellationToken);
            File.Delete(testFile);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public long EstimateBackupSizeBytes(double dataFileSizeMb)
    {
        // SQL Server Express does not support backup compression.
        // A full backup is roughly equal to the data file size; use 1.1× as a safety margin.
        return (long)(dataFileSizeMb * 1024 * 1024 * 1.1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string? GetDriveRoot(string path)
    {
        try
        {
            return string.IsNullOrWhiteSpace(path) ? null : Path.GetPathRoot(path);
        }
        catch { return null; }
    }

    private static bool TestWritable(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return false;
            var testFile = Path.Combine(path, $".vms_test_{Guid.NewGuid():N}");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch { return false; }
    }

    private static string DetermineAlertLevel(double freePercent, int warnPercent, int criticalPercent)
        => freePercent switch
        {
            var p when p <= criticalPercent => StorageAlertLevel.Critical,
            var p when p <= warnPercent => StorageAlertLevel.Warning,
            _ => StorageAlertLevel.None
        };
}
