using System.Collections.Concurrent;

namespace VisitorManagementSystem.Api.Infrastructure.Security.Encryption;

/// <summary>
/// Key management service implementation
/// </summary>
public class KeyManagementService : IKeyManagementService
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<KeyManagementService> _logger;
    private readonly ConcurrentDictionary<string, KeyInfo> _keys;
    private readonly object _rotationLock = new();
    private string _currentKeyId;

    public KeyManagementService(IEncryptionService encryptionService, ILogger<KeyManagementService> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
        _keys = new ConcurrentDictionary<string, KeyInfo>();
        _currentKeyId = InitializeDefaultKey();
    }

    public async Task<string> GetCurrentKeyAsync()
    {
        await Task.CompletedTask;

        if (_keys.TryGetValue(_currentKeyId, out var keyInfo))
        {
            return keyInfo.Key;
        }

        throw new InvalidOperationException("Current encryption key not found");
    }

    public async Task<string> RotateKeyAsync()
    {
        return await Task.Run(() =>
        {
            lock (_rotationLock)
            {
                var newKeyId = Guid.NewGuid().ToString();
                var newKey = _encryptionService.GenerateKey();

                var keyInfo = new KeyInfo
                {
                    Id = newKeyId,
                    Key = newKey,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    RotationReason = "Scheduled rotation"
                };

                _keys.TryAdd(newKeyId, keyInfo);

                // Deactivate old key
                if (_keys.TryGetValue(_currentKeyId, out var oldKey))
                {
                    oldKey.IsActive = false;
                    oldKey.DeactivatedAt = DateTime.UtcNow;
                }

                _currentKeyId = newKeyId;

                _logger.LogInformation("Encryption key rotated. New key ID: {KeyId}", newKeyId);

                return newKey;
            }
        });
    }

    public async Task<string?> GetKeyByIdAsync(string keyId)
    {
        await Task.CompletedTask;

        if (_keys.TryGetValue(keyId, out var keyInfo))
        {
            return keyInfo.Key;
        }

        return null;
    }

    public async Task<Dictionary<string, string>> GetActiveKeysAsync()
    {
        await Task.CompletedTask;

        return _keys
            .Where(kvp => kvp.Value.IsActive)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Key);
    }

    public async Task<bool> DeactivateKeyAsync(string keyId)
    {
        await Task.CompletedTask;

        if (_keys.TryGetValue(keyId, out var keyInfo))
        {
            keyInfo.IsActive = false;
            keyInfo.DeactivatedAt = DateTime.UtcNow;

            _logger.LogInformation("Encryption key deactivated. Key ID: {KeyId}", keyId);
            return true;
        }

        return false;
    }

    public async Task<bool> ValidateKeyAsync(string keyId)
    {
        await Task.CompletedTask;

        if (_keys.TryGetValue(keyId, out var keyInfo))
        {
            // Validate key format and length
            try
            {
                var keyBytes = Convert.FromBase64String(keyInfo.Key);
                return keyBytes.Length == 32; // 256-bit key
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    public async Task<DateTime?> GetNextRotationDateAsync()
    {
        await Task.CompletedTask;

        if (_keys.TryGetValue(_currentKeyId, out var keyInfo))
        {
            return keyInfo.CreatedAt.AddDays(90); // Rotate every 90 days
        }

        return null;
    }

    public async Task<string> ForceRotationAsync(string reason)
    {
        return await Task.Run(() =>
        {
            lock (_rotationLock)
            {
                var newKeyId = Guid.NewGuid().ToString();
                var newKey = _encryptionService.GenerateKey();

                var keyInfo = new KeyInfo
                {
                    Id = newKeyId,
                    Key = newKey,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    RotationReason = $"Force rotation: {reason}"
                };

                _keys.TryAdd(newKeyId, keyInfo);

                // Deactivate old key
                if (_keys.TryGetValue(_currentKeyId, out var oldKey))
                {
                    oldKey.IsActive = false;
                    oldKey.DeactivatedAt = DateTime.UtcNow;
                    oldKey.RotationReason = $"Replaced due to: {reason}";
                }

                _currentKeyId = newKeyId;

                _logger.LogWarning("Encryption key force rotated. Reason: {Reason}, New key ID: {KeyId}", reason, newKeyId);

                return newKey;
            }
        });
    }

    private string InitializeDefaultKey()
    {
        var keyId = Guid.NewGuid().ToString();
        var key = _encryptionService.GenerateKey();

        var keyInfo = new KeyInfo
        {
            Id = keyId,
            Key = key,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            RotationReason = "Initial key"
        };

        _keys.TryAdd(keyId, keyInfo);

        return keyId;
    }
}

/// <summary>
/// Key information structure
/// </summary>
internal class KeyInfo
{
    public string Id { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public bool IsActive { get; set; }
    public string RotationReason { get; set; } = string.Empty;
}