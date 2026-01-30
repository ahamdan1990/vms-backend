using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace VisitorManagementSystem.Api.Infrastructure.Caching;

/// <summary>
/// Background service that monitors Redis availability and notifies the resilient cache
/// </summary>
public class RedisHealthCheckService : BackgroundService
{
    private readonly ILogger<RedisHealthCheckService> _logger;
    private readonly ResilientCacheOptions _options;
    private readonly string? _redisConnectionString;
    private readonly ResilientDistributedCache _resilientCache;
    private bool _wasAvailableLastCheck = true;

    public RedisHealthCheckService(
        ILogger<RedisHealthCheckService> logger,
        IOptions<ResilientCacheOptions> options,
        IConfiguration configuration,
        ResilientDistributedCache resilientCache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _resilientCache = resilientCache ?? throw new ArgumentNullException(nameof(resilientCache));

        // Get Redis connection string from environment variable or configuration
        _redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
            ?? configuration.GetConnectionString("Redis");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Only run if automatic recovery is enabled and Redis is configured
        if (!_options.EnableAutomaticRecovery || string.IsNullOrWhiteSpace(_redisConnectionString))
        {
            _logger.LogInformation("Redis health check service disabled (AutomaticRecovery: {Enabled}, Redis configured: {Configured})",
                _options.EnableAutomaticRecovery,
                !string.IsNullOrWhiteSpace(_redisConnectionString));
            return;
        }

        _logger.LogInformation("Redis health check service started. Checking every {Interval} seconds", _options.HealthCheckIntervalSeconds);

        // Initial delay before starting health checks
        await Task.Delay(TimeSpan.FromSeconds(_options.InitialHealthCheckDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var isAvailable = await CheckRedisAvailability(stoppingToken);

                // Only log and notify on state changes
                if (isAvailable && !_wasAvailableLastCheck)
                {
                    _logger.LogInformation("Redis is now AVAILABLE - notifying resilient cache to switch back to Redis");
                    _resilientCache.NotifyRedisAvailable();
                }
                else if (!isAvailable && _wasAvailableLastCheck)
                {
                    _logger.LogWarning("Redis is now UNAVAILABLE - notifying resilient cache to use memory cache");
                    _resilientCache.NotifyRedisUnavailable();
                }

                _wasAvailableLastCheck = isAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Redis health check");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.HealthCheckIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Redis health check service stopped");
    }

    private async Task<bool> CheckRedisAvailability(CancellationToken cancellationToken)
    {
        IConnectionMultiplexer? connection = null;

        try
        {
            var options = ConfigurationOptions.Parse(_redisConnectionString!);
            options.ConnectTimeout = 2000; // 2 second timeout
            options.SyncTimeout = 2000;
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 1;

            connection = await ConnectionMultiplexer.ConnectAsync(options);

            if (!connection.IsConnected)
            {
                return false;
            }

            // Try to ping Redis
            var database = connection.GetDatabase();
            var pingResult = await database.PingAsync();

            var isHealthy = pingResult.TotalMilliseconds < 1000; // Consider healthy if ping < 1 second

            if (!isHealthy)
            {
                _logger.LogDebug("Redis ping slow: {PingMs}ms", pingResult.TotalMilliseconds);
            }

            return isHealthy;
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogDebug("Redis connection failed during health check: {Message}", ex.Message);
            return false;
        }
        catch (TimeoutException ex)
        {
            _logger.LogDebug("Redis health check timed out: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Redis health check failed with unexpected error");
            return false;
        }
        finally
        {
            if (connection != null)
            {
                try
                {
                    await connection.CloseAsync();
                    connection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error disposing Redis connection during health check");
                }
            }
        }
    }
}
