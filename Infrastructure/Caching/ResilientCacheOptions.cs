namespace VisitorManagementSystem.Api.Infrastructure.Caching;

/// <summary>
/// Configuration options for the resilient distributed cache
/// </summary>
public class ResilientCacheOptions
{
    /// <summary>
    /// Section name in appsettings.json
    /// </summary>
    public const string SectionName = "ResilientCache";

    /// <summary>
    /// Interval between Redis health checks (in seconds)
    /// Default: 30 seconds
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout for Redis operations before falling back to memory cache (in milliseconds)
    /// Default: 2000ms (2 seconds)
    /// </summary>
    public int RedisOperationTimeoutMs { get; set; } = 2000;

    /// <summary>
    /// Maximum consecutive failures before switching to memory cache
    /// Default: 3
    /// </summary>
    public int MaxConsecutiveFailures { get; set; } = 3;

    /// <summary>
    /// Whether to enable automatic failover to memory cache
    /// Default: true
    /// </summary>
    public bool EnableAutomaticFailover { get; set; } = true;

    /// <summary>
    /// Whether to enable automatic recovery to Redis when available
    /// Default: true
    /// </summary>
    public bool EnableAutomaticRecovery { get; set; } = true;

    /// <summary>
    /// Initial delay before starting health checks (in seconds)
    /// Default: 10 seconds
    /// </summary>
    public int InitialHealthCheckDelaySeconds { get; set; } = 10;
}
