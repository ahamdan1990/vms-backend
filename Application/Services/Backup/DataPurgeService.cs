using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Backup;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Application.Services.Backup;

/// <summary>
/// Purges old database records (invitations, notifications, audit logs, occupancy logs)
/// to reclaim space within the SQL Server Express 10 GB data-file limit.
///
/// IMPORTANT: A BACKUP DATABASE must be taken before running a purge.
/// The purge hard-deletes rows; there is no undo without restoring a backup.
/// DB shrink (DBCC SHRINKDATABASE) is optional and runs after purge if configured.
/// </summary>
public class DataPurgeService : IDataPurgeService
{
    // Invitation statuses that represent finished visits — safe to purge after retention period.
    private static readonly InvitationStatus[] TerminalStatuses =
    [
        InvitationStatus.Rejected,
        InvitationStatus.Cancelled,
        InvitationStatus.Expired,
        InvitationStatus.Completed
    ];

    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DataPurgeService> _logger;

    public DataPurgeService(
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<DataPurgeService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Preview (dry-run counts)
    // ─────────────────────────────────────────────────────────────────────

    public async Task<PurgePreviewDto> GetPurgePreviewAsync(RetentionSettingsDto settings, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-settings.RetentionDays);

        var invCount = settings.PurgeInvitations
            ? await _context.Invitations
                .CountAsync(i => TerminalStatuses.Contains(i.Status) && i.CreatedOn < cutoff, ct)
            : 0;

        var notifCount = settings.PurgeNotifications
            ? await _context.NotificationAlerts
                .CountAsync(n => n.CreatedOn < cutoff, ct)
            : 0;

        var auditCount = settings.PurgeAuditLogs
            ? await _context.AuditLogs
                .CountAsync(a => a.CreatedOn < cutoff, ct)
            : 0;

        var occupancyCount = settings.PurgeOccupancyLogs
            ? await _context.OccupancyLogs
                .CountAsync(o => o.Date < cutoff.Date, ct)
            : 0;

        return new PurgePreviewDto(
            invCount, notifCount, auditCount, occupancyCount,
            invCount + notifCount + auditCount + occupancyCount,
            cutoff);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Execute purge
    // ─────────────────────────────────────────────────────────────────────

    public async Task<PurgeResultDto> ExecutePurgeAsync(RetentionSettingsDto settings, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-settings.RetentionDays);
        int invPurged = 0, notifPurged = 0, auditPurged = 0, occupancyPurged = 0;

        try
        {
            // ── Invitations ────────────────────────────────────────────────
            if (settings.PurgeInvitations)
            {
                // Collect IDs of terminal invitations older than cutoff
                var targetIds = await _context.Invitations
                    .Where(i => TerminalStatuses.Contains(i.Status) && i.CreatedOn < cutoff)
                    .Select(i => i.Id)
                    .ToListAsync(ct);

                if (targetIds.Count > 0)
                {
                    // Delete child records first (FK constraints)
                    await _context.InvitationApprovals
                        .Where(a => targetIds.Contains(a.InvitationId))
                        .ExecuteDeleteAsync(ct);

                    await _context.InvitationEvents
                        .Where(e => targetIds.Contains(e.InvitationId))
                        .ExecuteDeleteAsync(ct);

                    await _context.TimeSlotBookings
                        .Where(b => b.InvitationId.HasValue && targetIds.Contains(b.InvitationId.Value))
                        .ExecuteDeleteAsync(ct);

                    invPurged = await _context.Invitations
                        .Where(i => targetIds.Contains(i.Id))
                        .ExecuteDeleteAsync(ct);

                    _logger.LogInformation("Purged {Count} terminal invitations (cutoff {Cutoff:yyyy-MM-dd})", invPurged, cutoff);
                }
            }

            // ── Notifications ──────────────────────────────────────────────
            if (settings.PurgeNotifications)
            {
                notifPurged = await _context.NotificationAlerts
                    .Where(n => n.CreatedOn < cutoff)
                    .ExecuteDeleteAsync(ct);

                _logger.LogInformation("Purged {Count} notification alerts (cutoff {Cutoff:yyyy-MM-dd})", notifPurged, cutoff);
            }

            // ── Audit logs ─────────────────────────────────────────────────
            if (settings.PurgeAuditLogs)
            {
                auditPurged = await _context.AuditLogs
                    .Where(a => a.CreatedOn < cutoff)
                    .ExecuteDeleteAsync(ct);

                _logger.LogInformation("Purged {Count} audit log entries (cutoff {Cutoff:yyyy-MM-dd})", auditPurged, cutoff);
            }

            // ── Occupancy logs ─────────────────────────────────────────────
            if (settings.PurgeOccupancyLogs)
            {
                occupancyPurged = await _context.OccupancyLogs
                    .Where(o => o.Date < cutoff.Date)
                    .ExecuteDeleteAsync(ct);

                _logger.LogInformation("Purged {Count} occupancy log entries (cutoff {Cutoff:yyyy-MM-dd})", occupancyPurged, cutoff);
            }

            // ── Optional: shrink data files ────────────────────────────────
            if (settings.ShrinkDbAfterPurge)
            {
                _logger.LogInformation("Running DBCC SHRINKDATABASE after purge...");
                _context.Database.SetCommandTimeout(300); // 5-minute timeout
                // Leave 10% free space in each file; avoids immediate re-growth thrash
                await _context.Database.ExecuteSqlRawAsync("DBCC SHRINKDATABASE (0, 10)");
                _logger.LogInformation("DBCC SHRINKDATABASE completed");
            }

            var total = invPurged + notifPurged + auditPurged + occupancyPurged;
            _logger.LogInformation("Data purge complete: {Total} total rows removed", total);

            return new PurgeResultDto(
                true, invPurged, notifPurged, auditPurged, occupancyPurged,
                total, null, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data purge failed");
            return new PurgeResultDto(false, invPurged, notifPurged, auditPurged, occupancyPurged,
                invPurged + notifPurged + auditPurged + occupancyPurged,
                ex.Message.Length > 500 ? ex.Message[..500] : ex.Message,
                DateTime.UtcNow);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Settings (read / write via SystemConfiguration)
    // ─────────────────────────────────────────────────────────────────────

    public async Task<RetentionSettingsDto> GetRetentionSettingsAsync(CancellationToken ct = default)
    {
        async Task<string> S(string key, string def) =>
            (await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync("Retention", key, ct))?.Value ?? def;
        async Task<bool> B(string key, bool def) =>
            bool.TryParse(await S(key, def.ToString()), out var v) ? v : def;
        async Task<int> I(string key, int def) =>
            int.TryParse(await S(key, def.ToString()), out var v) ? v : def;

        var nextRunRaw = await S("NextRunAt", "");
        DateTime? nextRun = DateTime.TryParse(nextRunRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var nr) ? nr : null;

        return new RetentionSettingsDto(
            AutoPurgeAfterBackup: await B("AutoPurgeAfterBackup", false),
            ScheduleEnabled:      await B("ScheduleEnabled", false),
            ScheduleTime:         await S("ScheduleTime", "03:00"),
            ScheduleDayOfWeek:    await S("ScheduleDayOfWeek", "Sunday"),
            RetentionDays:        await I("RetentionDays", 90),
            PurgeInvitations:     await B("PurgeInvitations", true),
            PurgeNotifications:   await B("PurgeNotifications", true),
            PurgeAuditLogs:       await B("PurgeAuditLogs", false),
            PurgeOccupancyLogs:   await B("PurgeOccupancyLogs", true),
            ShrinkDbAfterPurge:   await B("ShrinkDbAfterPurge", false),
            NextRunAt:            nextRun
        );
    }

    public async Task<(bool Success, string? Error)> UpdateRetentionSettingsAsync(
        UpdateRetentionSettingsRequest request, CancellationToken ct = default)
    {
        if (request.RetentionDays < 7 || request.RetentionDays > 3650)
            return (false, "RetentionDays must be between 7 and 3650.");

        if (!TimeSpan.TryParse(request.ScheduleTime, out _))
            return (false, "ScheduleTime must be in HH:mm format.");

        var validDays = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Daily" };
        if (!validDays.Contains(request.ScheduleDayOfWeek, StringComparer.OrdinalIgnoreCase))
            return (false, "ScheduleDayOfWeek must be a day name or 'Daily'.");

        await Set("AutoPurgeAfterBackup", request.AutoPurgeAfterBackup.ToString(), ct);
        await Set("ScheduleEnabled",      request.ScheduleEnabled.ToString(), ct);
        await Set("ScheduleTime",         request.ScheduleTime, ct);
        await Set("ScheduleDayOfWeek",    request.ScheduleDayOfWeek, ct);
        await Set("RetentionDays",        request.RetentionDays.ToString(), ct);
        await Set("PurgeInvitations",     request.PurgeInvitations.ToString(), ct);
        await Set("PurgeNotifications",   request.PurgeNotifications.ToString(), ct);
        await Set("PurgeAuditLogs",       request.PurgeAuditLogs.ToString(), ct);
        await Set("PurgeOccupancyLogs",   request.PurgeOccupancyLogs.ToString(), ct);
        await Set("ShrinkDbAfterPurge",   request.ShrinkDbAfterPurge.ToString(), ct);

        if (request.ScheduleEnabled)
        {
            var next = CalculateNextRunAt(request.ScheduleTime, request.ScheduleDayOfWeek);
            await Set("NextRunAt", next.ToString("O"), ct);
        }
        else
        {
            await Set("NextRunAt", string.Empty, ct);
        }

        return (true, null);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

    private async Task Set(string key, string value, CancellationToken ct)
    {
        var config = await _unitOfWork.SystemConfigurations.GetByCategoryAndKeyAsync("Retention", key, ct);
        if (config == null)
        {
            var dataType = bool.TryParse(value, out _) ? "boolean"
                         : int.TryParse(value, out _) ? "integer"
                         : "string";
            await _unitOfWork.Repository<SystemConfiguration>().AddAsync(new SystemConfiguration
            {
                Category = "Retention", Key = key, Value = value, DataType = dataType, Environment = "All"
            }, ct);
        }
        else
        {
            config.Value = value;
            config.UpdateModifiedOn();
            _unitOfWork.SystemConfigurations.Update(config);
        }
        await _unitOfWork.SaveChangesAsync(ct);
    }

    internal static DateTime CalculateNextRunAt(string scheduleTime, string dayOfWeek)
    {
        if (!TimeSpan.TryParse(scheduleTime, out var time)) time = TimeSpan.FromHours(3);

        var now = DateTime.UtcNow;
        var candidate = now.Date.Add(time);
        if (candidate <= now) candidate = candidate.AddDays(1);

        if (!string.Equals(dayOfWeek, "Daily", StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse<DayOfWeek>(dayOfWeek, true, out var targetDay))
        {
            while (candidate.DayOfWeek != targetDay)
                candidate = candidate.AddDays(1);
        }

        return candidate;
    }
}
