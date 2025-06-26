namespace VisitorManagementSystem.Api.Configuration;

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseConfiguration
{
    public const string SectionName = "Database";

    /// <summary>
    /// Primary database connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Read-only database connection string for read operations
    /// </summary>
    public string ReadOnlyConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Maximum retry count for database operations
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Whether to enable sensitive data logging (dev only)
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Whether to enable detailed errors
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// Connection pool settings
    /// </summary>
    public ConnectionPoolConfiguration ConnectionPool { get; set; } = new();

    /// <summary>
    /// Migration settings
    /// </summary>
    public MigrationConfiguration Migration { get; set; } = new();

    /// <summary>
    /// Backup settings
    /// </summary>
    public BackupConfiguration Backup { get; set; } = new();

    /// <summary>
    /// Performance monitoring settings
    /// </summary>
    public PerformanceConfiguration Performance { get; set; } = new();

    /// <summary>
    /// Health check settings
    /// </summary>
    public HealthCheckConfiguration HealthCheck { get; set; } = new();
}

/// <summary>
/// Connection pool configuration
/// </summary>
public class ConnectionPoolConfiguration
{
    public int MinPoolSize { get; set; } = 5;
    public int MaxPoolSize { get; set; } = 100;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromMinutes(30);
    public bool Pooling { get; set; } = true;
    public int ConnectionIdleTimeout { get; set; } = 300;
}

/// <summary>
/// Migration configuration
/// </summary>
public class MigrationConfiguration
{
    public bool AutoMigrate { get; set; } = false;
    public bool ValidateOnStartup { get; set; } = true;
    public string MigrationAssembly { get; set; } = string.Empty;
    public string MigrationHistoryTable { get; set; } = "__EFMigrationsHistory";
    public string BackupBeforeMigration { get; set; } = string.Empty;
}

/// <summary>
/// Backup configuration
/// </summary>
public class BackupConfiguration
{
    public bool EnableAutomaticBackups { get; set; } = false;
    public string BackupDirectory { get; set; } = string.Empty;
    public TimeSpan BackupInterval { get; set; } = TimeSpan.FromHours(24);
    public int RetentionDays { get; set; } = 30;
    public bool CompressBackups { get; set; } = true;
    public bool EncryptBackups { get; set; } = true;
}

/// <summary>
/// Performance configuration
/// </summary>
public class PerformanceConfiguration
{
    public bool EnableQueryCache { get; set; } = true;
    public int QueryCacheSize { get; set; } = 1000;
    public TimeSpan QueryCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableConnectionResiliency { get; set; } = true;
    public bool LogSlowQueries { get; set; } = true;
    public TimeSpan SlowQueryThreshold { get; set; } = TimeSpan.FromSeconds(1);
    public bool EnableStatistics { get; set; } = true;
}

/// <summary>
/// Health check configuration
/// </summary>
public class HealthCheckConfiguration
{
    public bool Enabled { get; set; } = true;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);
    public string TestQuery { get; set; } = "SELECT 1";
    public bool CheckReadOnlyConnection { get; set; } = true;
    public bool CheckWriteConnection { get; set; } = true;
}
