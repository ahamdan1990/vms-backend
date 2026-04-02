using VisitorManagementSystem.Api.Application.DTOs.Backup;

namespace VisitorManagementSystem.Api.Application.Services.Backup;

/// <summary>
/// Inspects Windows drives using System.IO.DriveInfo.
/// Checks free space on the database drive and the backup destination drive.
/// Also validates whether the backup destination path is writable.
/// </summary>
public interface IDiskStorageMonitor
{
    /// <summary>
    /// Returns health data for the drives used by the database and backup destination.
    /// Drive letters are extracted from the file physical paths returned by IDatabaseStorageMonitor.
    /// </summary>
    Task<List<DriveHealthDto>> GetDriveHealthAsync(
        IEnumerable<string> dbFilePaths,
        string backupDestinationPath,
        int warnFreePercent,
        int criticalFreePercent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests whether the given directory path exists and is writable by the application process.
    /// Returns (isWritable, errorMessage).
    /// </summary>
    Task<(bool IsWritable, string? Error)> TestPathWritableAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns estimated disk space required for the next backup in bytes.
    /// Uses current data file size * 0.8 as conservative estimate (assumes compression).
    /// </summary>
    long EstimateBackupSizeBytes(double dataFileSizeMb);
}
