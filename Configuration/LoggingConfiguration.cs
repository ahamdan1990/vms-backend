namespace VisitorManagementSystem.Api.Configuration;

/// <summary>
/// Logging configuration settings
/// </summary>
public class LoggingConfiguration
{
    public const string SectionName = "Logging";

    /// <summary>
    /// Global log level
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Whether to enable structured logging
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;

    /// <summary>
    /// Whether to enable correlation ID logging
    /// </summary>
    public bool EnableCorrelationId { get; set; } = true;

    /// <summary>
    /// Whether to log request/response details
    /// </summary>
    public bool LogRequestResponse { get; set; } = true;

    /// <summary>
    /// Whether to log performance metrics
    /// </summary>
    public bool LogPerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Console logging settings
    /// </summary>
    public ConsoleLoggingConfiguration Console { get; set; } = new();

    /// <summary>
    /// File logging settings
    /// </summary>
    public FileLoggingConfiguration File { get; set; } = new();

    /// <summary>
    /// Database logging settings
    /// </summary>
    public DatabaseLoggingConfiguration Database { get; set; } = new();

    /// <summary>
    /// Audit logging settings
    /// </summary>
    public AuditLoggingConfiguration Audit { get; set; } = new();

    /// <summary>
    /// Security logging settings
    /// </summary>
    public SecurityLoggingConfiguration Security { get; set; } = new();

    /// <summary>
    /// Application Insights settings
    /// </summary>
    public ApplicationInsightsConfiguration ApplicationInsights { get; set; } = new();

    /// <summary>
    /// Sensitive data logging settings
    /// </summary>
    public SensitiveDataConfiguration SensitiveData { get; set; } = new();
}

/// <summary>
/// Console logging configuration
/// </summary>
public class ConsoleLoggingConfiguration
{
    public bool Enabled { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
    public string OutputTemplate { get; set; } = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}";
    public bool UseColors { get; set; } = true;
    public bool IncludeScopes { get; set; } = true;
}

/// <summary>
/// File logging configuration
/// </summary>
public class FileLoggingConfiguration
{
    public bool Enabled { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
    public string Path { get; set; } = "logs/vms-.txt";
    public string OutputTemplate { get; set; } = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {CorrelationId} {Message:lj} <s:{SourceContext}>{NewLine}{Exception}";
    public string RollingInterval { get; set; } = "Day";
    public int RetainedFileCountLimit { get; set; } = 30;
    public long? FileSizeLimitBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    public bool RollOnFileSizeLimit { get; set; } = true;
    public bool Shared { get; set; } = false;
    public bool FlushToDiskInterval { get; set; } = true;
}

/// <summary>
/// Database logging configuration
/// </summary>
public class DatabaseLoggingConfiguration
{
    public bool Enabled { get; set; } = false;
    public string LogLevel { get; set; } = "Warning";
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = "Logs";
    public string SchemaName { get; set; } = "dbo";
    public bool AutoCreateSqlTable { get; set; } = true;
    public TimeSpan BatchPostingLimit { get; set; } = TimeSpan.FromSeconds(2);
    public int BatchSizeLimit { get; set; } = 1000;
}

/// <summary>
/// Audit logging configuration
/// </summary>
public class AuditLoggingConfiguration
{
    public bool Enabled { get; set; } = true;
    public string LogLevel { get; set; } = "Information";
    public bool LogUserActions { get; set; } = true;
    public bool LogDataChanges { get; set; } = true;
    public bool LogSystemEvents { get; set; } = true;
    public bool LogSecurityEvents { get; set; } = true;
    public bool LogLoginAttempts { get; set; } = true;
    public bool LogApiRequests { get; set; } = true;
    public List<string> ExcludedPaths { get; set; } = new() { "/health", "/metrics" };
    public List<string> ExcludedHeaders { get; set; } = new() { "Authorization", "Cookie" };
    public int RetentionDays { get; set; } = 365;
}

/// <summary>
/// Security logging configuration
/// </summary>
public class SecurityLoggingConfiguration
{
    public bool Enabled { get; set; } = true;
    public string LogLevel { get; set; } = "Warning";
    public bool LogFailedAuthentication { get; set; } = true;
    public bool LogUnauthorizedAccess { get; set; } = true;
    public bool LogSuspiciousActivity { get; set; } = true;
    public bool LogPasswordChanges { get; set; } = true;
    public bool LogAccountLockouts { get; set; } = true;
    public bool LogTokenEvents { get; set; } = true;
    public bool LogPermissionChanges { get; set; } = true;
    public bool AlertOnSecurityEvents { get; set; } = true;
    public List<string> MonitoredEvents { get; set; } = new() { "Login", "Logout", "PasswordChange", "AccountLockout" };
}

/// <summary>
/// Application Insights configuration
/// </summary>
public class ApplicationInsightsConfiguration
{
    public bool Enabled { get; set; } = false;
    public string InstrumentationKey { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string CloudRole { get; set; } = "VMS-API";
    public string CloudRoleInstance { get; set; } = Environment.MachineName;
    public bool EnableRequestTracking { get; set; } = true;
    public bool EnableDependencyTracking { get; set; } = true;
    public bool EnablePerformanceTracking { get; set; } = true;
    public bool EnableExceptionTracking { get; set; } = true;
    public bool EnableHeartbeat { get; set; } = true;
    public double SamplingPercentage { get; set; } = 100.0;
}

/// <summary>
/// Sensitive data logging configuration
/// </summary>
public class SensitiveDataConfiguration
{
    public bool LogSensitiveData { get; set; } = false;
    public bool MaskSensitiveData { get; set; } = true;
    public List<string> SensitiveFields { get; set; } = new()
    {
        "Password", "PasswordHash", "Salt", "Token", "RefreshToken",
        "SecurityStamp", "Email", "PhoneNumber", "SSN", "CreditCard"
    };
    public string MaskCharacter { get; set; } = "*";
    public int UnmaskedCharacters { get; set; } = 2;
    public bool LogDataLength { get; set; } = true;
}