using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Backup;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Application.Services.Backup;

public class DatabaseStorageMonitor : IDatabaseStorageMonitor
{
    private const double ExpressLimitMb = 10240.0; // SQL Server Express 2008 R2+: 10 GB data file limit
    private const int SqlExpressEngineEdition = 4;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseStorageMonitor> _logger;

    public DatabaseStorageMonitor(ApplicationDbContext context, ILogger<DatabaseStorageMonitor> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DatabaseStorageDto> GetStorageMetricsAsync(CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        bool opened = false;

        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
                opened = true;
            }

            // Query 1: database files
            var (files, totalDataAllocated, totalDataUsed, totalLogAllocated) =
                await QueryDatabaseFilesAsync(connection, cancellationToken);

            // Query 2: edition + recovery model
            var (isExpress, edition, recoveryModel) =
                await QueryEditionAsync(connection, cancellationToken);

            double usagePercent = isExpress && ExpressLimitMb > 0
                ? Math.Round(totalDataAllocated / ExpressLimitMb * 100, 1)
                : 0;

            return new DatabaseStorageDto
            {
                IsExpressEdition = isExpress,
                ExpressLimitMb = ExpressLimitMb,
                TotalDataAllocatedMb = Math.Round(totalDataAllocated, 1),
                TotalDataUsedMb = Math.Round(totalDataUsed, 1),
                TotalLogAllocatedMb = Math.Round(totalLogAllocated, 1),
                UsagePercent = usagePercent,
                AlertLevel = DetermineAlertLevel(usagePercent, isExpress),
                RecoveryModel = recoveryModel,
                SqlEdition = edition,
                Files = files
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query database storage metrics");
            throw;
        }
        finally
        {
            if (opened) await connection.CloseAsync();
        }
    }

    private static async Task<(List<DatabaseFileInfoDto> Files, double DataAllocated, double DataUsed, double LogAllocated)>
        QueryDatabaseFilesAsync(System.Data.Common.DbConnection connection, CancellationToken ct)
    {
        var files = new List<DatabaseFileInfoDto>();
        double dataAllocated = 0, dataUsed = 0, logAllocated = 0;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT
    f.name                                                              AS FileName,
    f.type_desc                                                         AS FileType,
    CAST(f.size * 8.0 / 1024.0 AS float)                               AS AllocatedMb,
    CAST(ISNULL(FILEPROPERTY(f.name,'SpaceUsed'), 0) * 8.0/1024.0 AS float) AS UsedMb,
    f.physical_name                                                     AS PhysicalPath,
    f.max_size                                                          AS MaxSize,
    f.growth                                                            AS Growth,
    f.is_percent_growth                                                 AS IsPercentGrowth
FROM sys.database_files f
ORDER BY f.type, f.file_id;";

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var fileType    = reader.GetString(1);
            var allocated   = reader.GetDouble(2);
            var used        = reader.GetDouble(3);
            var physicalPath = reader.GetString(4);
            var maxSize     = reader.GetInt32(5);
            var growth      = reader.GetInt32(6);
            var isPct       = reader.GetBoolean(7);

            var growthSetting = isPct ? $"{growth}% auto-growth"
                              : growth == 0 ? "No auto-growth"
                              : $"{growth * 8 / 1024} MB auto-growth";

            files.Add(new DatabaseFileInfoDto(
                FileName: reader.GetString(0),
                FileType: fileType,
                AllocatedMb: Math.Round(allocated, 1),
                UsedMb: Math.Round(used, 1),
                FreeMbInsideFile: Math.Round(allocated - used, 1),
                PhysicalPath: physicalPath,
                GrowthSetting: growthSetting,
                IsMaxSizeLimited: maxSize > 0
            ));

            if (fileType == "ROWS") { dataAllocated += allocated; dataUsed += used; }
            else if (fileType == "LOG") logAllocated += allocated;
        }

        return (files, dataAllocated, dataUsed, logAllocated);
    }

    private static async Task<(bool IsExpress, string Edition, string RecoveryModel)>
        QueryEditionAsync(System.Data.Common.DbConnection connection, CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT
    CAST(SERVERPROPERTY('Edition')       AS nvarchar(128)) AS Edition,
    CAST(SERVERPROPERTY('EngineEdition') AS int)           AS EngineEdition,
    d.recovery_model_desc
FROM sys.databases d
WHERE d.database_id = DB_ID();";

        using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var edition = reader.IsDBNull(0) ? "Unknown" : reader.GetString(0);
            var engineEdition = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            var recoveryModel = reader.IsDBNull(2) ? "SIMPLE" : reader.GetString(2);
            return (engineEdition == SqlExpressEngineEdition, edition, recoveryModel);
        }

        return (false, "Unknown", "SIMPLE");
    }

    private static string DetermineAlertLevel(double usagePercent, bool isExpress)
    {
        if (!isExpress) return StorageAlertLevel.None;
        return usagePercent switch
        {
            >= 95 => StorageAlertLevel.Critical,
            >= 85 => StorageAlertLevel.High,
            >= 70 => StorageAlertLevel.Warning,
            _ => StorageAlertLevel.None
        };
    }
}
