using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Records each backup operation — scheduled, manual, or event-triggered.
/// These records are never deleted; they are the permanent operational history.
/// </summary>
public class BackupRecord : AuditableEntity
{
    /// <summary>
    /// What triggered this backup: Scheduled, Manual, StorageAlert
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string TriggerType { get; set; } = string.Empty;

    /// <summary>
    /// Who triggered it — user ID for manual, null for system-triggered
    /// </summary>
    public int? TriggeredByUserId { get; set; }

    /// <summary>
    /// Display name for the trigger source (username or "System")
    /// </summary>
    [MaxLength(100)]
    public string TriggeredByDisplay { get; set; } = "System";

    /// <summary>
    /// When the backup job started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the backup job finished (null if still running or failed before completion)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Current status: Running, Completed, Failed, Cancelled
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = BackupStatus.Running;

    /// <summary>
    /// Full absolute path of the resulting .bak file
    /// </summary>
    [MaxLength(500)]
    public string? FilePath { get; set; }

    /// <summary>
    /// File name only, for display
    /// </summary>
    [MaxLength(200)]
    public string? FileName { get; set; }

    /// <summary>
    /// Size of the .bak file in bytes (0 if not completed)
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Duration in seconds (0 if not completed)
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Error message if Status = Failed
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Database name that was backed up
    /// </summary>
    [MaxLength(128)]
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Data file size in MB at the time of backup (for trend tracking)
    /// </summary>
    public double DataFileSizeMbAtBackup { get; set; }

    /// <summary>
    /// Navigation: the user who triggered a manual backup
    /// </summary>
    public virtual User? TriggeredByUser { get; set; }

    // ── Computed helpers ──────────────────────────────────────────────────

    public bool IsCompleted => Status == BackupStatus.Completed;
    public bool IsFailed => Status == BackupStatus.Failed;
    public bool IsRunning => Status == BackupStatus.Running;

    public string FileSizeDisplay => FileSizeBytes switch
    {
        0 => "—",
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{FileSizeBytes / (1024.0 * 1024):F1} MB",
        _ => $"{FileSizeBytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    public string DurationDisplay => DurationSeconds switch
    {
        0 => "—",
        < 60 => $"{DurationSeconds}s",
        < 3600 => $"{DurationSeconds / 60}m {DurationSeconds % 60}s",
        _ => $"{DurationSeconds / 3600}h {(DurationSeconds % 3600) / 60}m"
    };

    // ── Factory & state transitions ───────────────────────────────────────

    public static BackupRecord Start(string triggerType, int? userId, string triggeredByDisplay, string databaseName, double dataFileSizeMb)
        => new()
        {
            TriggerType = triggerType,
            TriggeredByUserId = userId,
            TriggeredByDisplay = triggeredByDisplay,
            StartedAt = DateTime.UtcNow,
            Status = BackupStatus.Running,
            DatabaseName = databaseName,
            DataFileSizeMbAtBackup = dataFileSizeMb,
        };

    public void Complete(string filePath, string fileName, long fileSizeBytes)
    {
        Status = BackupStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        FilePath = filePath;
        FileName = fileName;
        FileSizeBytes = fileSizeBytes;
        DurationSeconds = (int)(CompletedAt.Value - StartedAt).TotalSeconds;
        UpdateModifiedOn();
    }

    public void Fail(string errorMessage)
    {
        Status = BackupStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        DurationSeconds = (int)(CompletedAt.Value - StartedAt).TotalSeconds;
        UpdateModifiedOn();
    }
}

/// <summary>
/// Backup trigger source constants
/// </summary>
public static class BackupTriggerType
{
    public const string Manual = "Manual";
    public const string Scheduled = "Scheduled";
    public const string StorageAlert = "StorageAlert";
}

/// <summary>
/// Backup job status constants
/// </summary>
public static class BackupStatus
{
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
