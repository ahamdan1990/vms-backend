namespace VisitorManagementSystem.Api.Application.DTOs.Backup;

// ─────────────────────────────────────────────────────────────────────────────
// Storage health
// ─────────────────────────────────────────────────────────────────────────────

public record DatabaseFileInfoDto(
    string FileName,
    string FileType,
    double AllocatedMb,
    double UsedMb,
    double FreeMbInsideFile,
    string PhysicalPath,
    string GrowthSetting,
    bool IsMaxSizeLimited
);

public record DatabaseStorageDto
{
    public bool IsExpressEdition { get; init; }
    public double ExpressLimitMb { get; init; } = 10240;
    public double TotalDataAllocatedMb { get; init; }
    public double TotalDataUsedMb { get; init; }
    public double TotalLogAllocatedMb { get; init; }
    public double UsagePercent { get; init; }
    public string AlertLevel { get; init; } = StorageAlertLevel.None;
    public string RecoveryModel { get; init; } = string.Empty;
    public string SqlEdition { get; init; } = string.Empty;
    public List<DatabaseFileInfoDto> Files { get; init; } = new();
}

public record DriveHealthDto(
    string DriveLetter,
    string Purpose,
    long TotalBytes,
    long FreeBytes,
    double FreePercent,
    bool IsWritable,
    string AlertLevel
);

// ─────────────────────────────────────────────────────────────────────────────
// Backup status
// ─────────────────────────────────────────────────────────────────────────────

public record BackupJobStateDto(
    bool IsRunning,
    string? CurrentTriggerType,
    DateTime? StartedAt
);

public record BackupRecordDto(
    int Id,
    string TriggerType,
    string TriggeredByDisplay,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string Status,
    string? FileName,
    string? FilePath,
    string FileSizeDisplay,
    string DurationDisplay,
    string? ErrorMessage
);

// ─────────────────────────────────────────────────────────────────────────────
// Settings
// ─────────────────────────────────────────────────────────────────────────────

public record BackupSettingsDto(
    bool Enabled,
    string ScheduleTime,
    string DestinationPath,
    int RetentionDays,
    bool AutoBackupOnAlert,
    bool AutoBackupOnCritical,
    bool AlertEnabled,
    int DbWarnThresholdPercent,
    int DbAlertThresholdPercent,
    int DbCriticalThresholdPercent,
    int DiskWarnFreePercent,
    int DiskCriticalFreePercent
);

public record UpdateBackupSettingsRequest(
    bool Enabled,
    string ScheduleTime,
    string DestinationPath,
    int RetentionDays,
    bool AutoBackupOnAlert,
    bool AutoBackupOnCritical,
    bool AlertEnabled,
    int DbWarnThresholdPercent,
    int DbAlertThresholdPercent,
    int DbCriticalThresholdPercent,
    int DiskWarnFreePercent,
    int DiskCriticalFreePercent
);

// ─────────────────────────────────────────────────────────────────────────────
// Combined health response (single endpoint used by the page)
// ─────────────────────────────────────────────────────────────────────────────

public record BackupHealthDto
{
    public DatabaseStorageDto Database { get; init; } = null!;
    public List<DriveHealthDto> Drives { get; init; } = new();
    public BackupJobStateDto JobState { get; init; } = null!;
    public BackupRecordDto? LastSuccess { get; init; }
    public BackupRecordDto? LastFailure { get; init; }
    public DateTime? NextScheduledAt { get; init; }
    public BackupSettingsDto Settings { get; init; } = null!;
    public List<string> ActiveWarnings { get; init; } = new();
    public string OverallStatus { get; init; } = OverallHealthStatus.Healthy;
}

// ─────────────────────────────────────────────────────────────────────────────
// SignalR storage alert payload (sent to AdminHub on threshold crossing)
// ─────────────────────────────────────────────────────────────────────────────

public record StorageAlertPayload(
    string AlertType,
    string AlertLevel,
    double UsedPercent,
    double UsedMb,
    double LimitMb,
    string DrivePath,
    bool BackupTriggered,
    DateTime Timestamp
);

// ─────────────────────────────────────────────────────────────────────────────
// Constants used across the backup system
// ─────────────────────────────────────────────────────────────────────────────

public static class StorageAlertLevel
{
    public const string None = "None";
    public const string Warning = "Warning";
    public const string High = "High";
    public const string Critical = "Critical";
}

public static class OverallHealthStatus
{
    public const string Healthy = "Healthy";
    public const string Warning = "Warning";
    public const string Critical = "Critical";
}
