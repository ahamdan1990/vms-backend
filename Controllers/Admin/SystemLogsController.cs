using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.DTOs.Backup;
using VisitorManagementSystem.Api.Application.Services.Backup;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers.Admin;

/// <summary>
/// Endpoints for managing the backend log files written by Serilog.
/// All routes require Administrator role.
/// </summary>
[ApiController]
[Route("api/system/logs")]
[Authorize(Policy = Permissions.Audit.ReadAll)]
public class SystemLogsController : ControllerBase
{
    private readonly ILogFileService _logService;
    private readonly ILogger<SystemLogsController> _logger;

    public SystemLogsController(ILogFileService logService, ILogger<SystemLogsController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/system/logs/health
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns log folder size, file count, oldest/newest file, and alert level.
    /// </summary>
    [HttpGet("health")]
    [Authorize(Policy = Permissions.Backup.View)]
    public async Task<IActionResult> GetLogHealth(CancellationToken cancellationToken)
    {
        try
        {
            var health = await _logService.GetLogFolderHealthAsync(cancellationToken);
            return Ok(new { success = true, data = health });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving log folder health");
            return StatusCode(500, new { success = false, message = "Failed to retrieve log folder health." });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // POST /api/system/logs/purge
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Deletes log files older than the configured retention period.
    /// </summary>
    [HttpPost("purge")]
    [Authorize(Policy = Permissions.Backup.Purge)]
    public async Task<IActionResult> PurgeLogs(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _logService.GetLogSettingsAsync(cancellationToken);
            _logger.LogInformation("Log purge initiated (retentionDays={Days})", settings.RetentionDays);

            var result = await _logService.PurgeOldLogsAsync(settings.RetentionDays, cancellationToken);

            if (!result.Success)
                return StatusCode(500, new { success = false, message = result.ErrorMessage, data = result });

            return Ok(new
            {
                success = true,
                message = $"{result.FilesDeleted} log file(s) deleted, {result.BytesFreedDisplay} freed.",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging log files");
            return StatusCode(500, new { success = false, message = "Log purge failed." });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // GET /api/system/logs/settings
    // PUT /api/system/logs/settings
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet("settings")]
    [Authorize(Policy = Permissions.Backup.View)]
    public async Task<IActionResult> GetLogSettings(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _logService.GetLogSettingsAsync(cancellationToken);
            return Ok(new { success = true, data = settings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving log settings");
            return StatusCode(500, new { success = false, message = "Failed to retrieve log settings." });
        }
    }

    [HttpPut("settings")]
    [Authorize(Policy = Permissions.Backup.Configure)]
    public async Task<IActionResult> UpdateLogSettings(
        [FromBody] UpdateLogSettingsRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { success = false, message = "Request body is required." });

        try
        {
            var (ok, error) = await _logService.UpdateLogSettingsAsync(request, cancellationToken);
            if (!ok) return BadRequest(new { success = false, message = error });

            return Ok(new { success = true, message = "Log settings saved." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating log settings");
            return StatusCode(500, new { success = false, message = "Failed to save log settings." });
        }
    }
}
