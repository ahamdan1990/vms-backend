using VisitorManagementSystem.Api.Application.DTOs.Backup;

namespace VisitorManagementSystem.Api.Application.Services.Backup;

/// <summary>
/// Queries SQL Server for database file sizes, growth settings, and edition information.
/// Results inform both the health dashboard and the storage alert threshold logic.
/// </summary>
public interface IDatabaseStorageMonitor
{
    /// <summary>
    /// Returns the current storage metrics for all database files.
    /// Uses sys.database_files and SERVERPROPERTY queries.
    /// Safe to call frequently — read-only, no locking impact.
    /// </summary>
    Task<DatabaseStorageDto> GetStorageMetricsAsync(CancellationToken cancellationToken = default);
}
