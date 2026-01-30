using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace VisitorManagementSystem.Api.Infrastructure.Caching;

/// <summary>
/// Resilient distributed cache implementation that falls back to memory cache when Redis is unavailable
/// </summary>
public class ResilientDistributedCache : IDistributedCache
{
    private readonly IDistributedCache _redisCache;
    private readonly IDistributedCache _memoryCache;
    private readonly ResilientCacheOptions _options;
    private readonly ILogger<ResilientDistributedCache> _logger;

    private int _consecutiveFailures = 0;
    private bool _isUsingRedis = true;
    private readonly object _lockObject = new object();
    private DateTime _lastFailureTime = DateTime.MinValue;

    public ResilientDistributedCache(
        IDistributedCache redisCache,
        IDistributedCache memoryCache,
        IOptions<ResilientCacheOptions> options,
        ILogger<ResilientDistributedCache> logger)
    {
        _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets whether the cache is currently using Redis
    /// </summary>
    public bool IsUsingRedis => _isUsingRedis;

    /// <summary>
    /// Notifies the cache that Redis is available (called by health check service)
    /// </summary>
    public void NotifyRedisAvailable()
    {
        if (!_options.EnableAutomaticRecovery)
            return;

        lock (_lockObject)
        {
            if (!_isUsingRedis)
            {
                _isUsingRedis = true;
                _consecutiveFailures = 0;
                _logger.LogInformation("Switched back to Redis cache - Redis is now available");
            }
        }
    }

    /// <summary>
    /// Notifies the cache that Redis is unavailable (called by health check service)
    /// </summary>
    public void NotifyRedisUnavailable()
    {
        SwitchToMemoryCache("Health check detected Redis is unavailable");
    }

    public byte[]? Get(string key)
    {
        return ExecuteWithFallback(
            () => _redisCache.Get(key),
            () => _memoryCache.Get(key),
            nameof(Get)
        );
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        return await ExecuteWithFallbackAsync(
            () => _redisCache.GetAsync(key, token),
            () => _memoryCache.GetAsync(key, token),
            nameof(GetAsync)
        );
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        ExecuteWithFallback(
            () => { _redisCache.Set(key, value, options); return true; },
            () => { _memoryCache.Set(key, value, options); return true; },
            nameof(Set)
        );
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        await ExecuteWithFallbackAsync(
            async () => { await _redisCache.SetAsync(key, value, options, token); return true; },
            async () => { await _memoryCache.SetAsync(key, value, options, token); return true; },
            nameof(SetAsync)
        );
    }

    public void Refresh(string key)
    {
        ExecuteWithFallback(
            () => { _redisCache.Refresh(key); return true; },
            () => { _memoryCache.Refresh(key); return true; },
            nameof(Refresh)
        );
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        await ExecuteWithFallbackAsync(
            async () => { await _redisCache.RefreshAsync(key, token); return true; },
            async () => { await _memoryCache.RefreshAsync(key, token); return true; },
            nameof(RefreshAsync)
        );
    }

    public void Remove(string key)
    {
        ExecuteWithFallback(
            () => { _redisCache.Remove(key); return true; },
            () => { _memoryCache.Remove(key); return true; },
            nameof(Remove)
        );
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        await ExecuteWithFallbackAsync(
            async () => { await _redisCache.RemoveAsync(key, token); return true; },
            async () => { await _memoryCache.RemoveAsync(key, token); return true; },
            nameof(RemoveAsync)
        );
    }

    private T ExecuteWithFallback<T>(Func<T> redisOperation, Func<T> memoryOperation, string operationName)
    {
        if (!_options.EnableAutomaticFailover || !_isUsingRedis)
        {
            return memoryOperation();
        }

        try
        {
            var task = Task.Run(redisOperation);
            if (task.Wait(TimeSpan.FromMilliseconds(_options.RedisOperationTimeoutMs)))
            {
                RecordSuccess();
                return task.Result;
            }
            else
            {
                RecordFailure($"Redis operation {operationName} timed out after {_options.RedisOperationTimeoutMs}ms");
                return memoryOperation();
            }
        }
        catch (Exception ex)
        {
            RecordFailure($"Redis operation {operationName} failed: {ex.Message}");
            return memoryOperation();
        }
    }

    private async Task<T> ExecuteWithFallbackAsync<T>(Func<Task<T>> redisOperation, Func<Task<T>> memoryOperation, string operationName)
    {
        if (!_options.EnableAutomaticFailover || !_isUsingRedis)
        {
            return await memoryOperation();
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_options.RedisOperationTimeoutMs));
            var task = redisOperation();

            if (await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token)) == task)
            {
                RecordSuccess();
                return await task;
            }
            else
            {
                RecordFailure($"Redis operation {operationName} timed out after {_options.RedisOperationTimeoutMs}ms");
                return await memoryOperation();
            }
        }
        catch (Exception ex)
        {
            RecordFailure($"Redis operation {operationName} failed: {ex.Message}");
            return await memoryOperation();
        }
    }

    private void RecordSuccess()
    {
        lock (_lockObject)
        {
            if (_consecutiveFailures > 0)
            {
                _consecutiveFailures = 0;
                _logger.LogInformation("Redis cache operations are now successful");
            }
        }
    }

    private void RecordFailure(string reason)
    {
        lock (_lockObject)
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTime.UtcNow;

            _logger.LogWarning("Redis cache failure #{FailureCount}: {Reason}", _consecutiveFailures, reason);

            if (_consecutiveFailures >= _options.MaxConsecutiveFailures && _isUsingRedis)
            {
                SwitchToMemoryCache($"Maximum consecutive failures ({_options.MaxConsecutiveFailures}) reached");
            }
        }
    }

    private void SwitchToMemoryCache(string reason)
    {
        lock (_lockObject)
        {
            if (_isUsingRedis)
            {
                _isUsingRedis = false;
                _logger.LogWarning("Switching to memory cache - Reason: {Reason}. Application will continue to function with in-memory caching.", reason);
            }
        }
    }
}
