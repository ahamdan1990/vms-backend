namespace VisitorManagementSystem.Api.Application.DTOs.Backup;

// ─────────────────────────────────────────────────────────────────────────────
// Data purge (database record retention)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Dry-run preview: how many rows would be deleted at the configured cutoff.
/// </summary>
public record PurgePreviewDto(
    int InvitationCount,
    int NotificationCount,
    int AuditLogCount,
    int OccupancyLogCount,
    int TotalCount,
    DateTime CutoffDate
);

public record PurgeResultDto(
    bool Success,
    int InvitationsPurged,
    int NotificationsPurged,
    int AuditLogsPurged,
    int OccupancyLogsPurged,
    int TotalPurged,
    string? ErrorMessage,
    DateTime ExecutedAt
);

public record RetentionSettingsDto(
    bool AutoPurgeAfterBackup,
    bool ScheduleEnabled,
    string ScheduleTime,
    string ScheduleDayOfWeek,
    int RetentionDays,
    bool PurgeInvitations,
    bool PurgeNotifications,
    bool PurgeAuditLogs,
    bool PurgeOccupancyLogs,
    bool ShrinkDbAfterPurge,
    DateTime? NextRunAt
);

public record UpdateRetentionSettingsRequest(
    bool AutoPurgeAfterBackup,
    bool ScheduleEnabled,
    string ScheduleTime,
    string ScheduleDayOfWeek,
    int RetentionDays,
    bool PurgeInvitations,
    bool PurgeNotifications,
    bool PurgeAuditLogs,
    bool PurgeOccupancyLogs,
    bool ShrinkDbAfterPurge
);

// ─────────────────────────────────────────────────────────────────────────────
// Log file management
// ─────────────────────────────────────────────────────────────────────────────

public record LogFileInfoDto(
    string FileName,
    long SizeBytes,
    string SizeDisplay,
    DateTime LastModified
);

public record LogFolderHealthDto(
    string FolderPath,
    bool FolderExists,
    long TotalBytes,
    string TotalSizeDisplay,
    int FileCount,
    DateTime? OldestFile,
    DateTime? NewestFile,
    List<LogFileInfoDto> Files,
    string AlertLevel
);

public record LogPurgeResultDto(
    bool Success,
    int FilesDeleted,
    long BytesFreed,
    string BytesFreedDisplay,
    string? ErrorMessage
);

public record LogSettingsDto(
    int RetentionDays,
    bool AutoPurgeEnabled,
    bool ScheduleEnabled,
    string ScheduleTime,
    DateTime? NextRunAt,
    int MaxFolderSizeMb
);

public record UpdateLogSettingsRequest(
    int RetentionDays,
    bool AutoPurgeEnabled,
    bool ScheduleEnabled,
    string ScheduleTime,
    int MaxFolderSizeMb
);
