using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using VisitorManagementSystem.Api.Application.Models.Licensing;
using VisitorManagementSystem.Api.Application.Services.Licensing;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Infrastructure.Configuration;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Infrastructure.Services.Licensing;

[SupportedOSPlatform("windows")]
public class LicenseStorageService : ILicenseStorageService
{
    private const string RegistryKeyPath = @"SOFTWARE\VMS\License";
    private const string RegValueLicenseId = "LicenseId";
    private const string RegValueChecksum = "Checksum";
    private const string RegValueUpdatedAt = "UpdatedAt";
    private const string KdfSalt = "VMS-License-v1";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LicenseStorageService> _logger;
    private readonly string _licensePath;

    public LicenseStorageService(IServiceScopeFactory scopeFactory, ILogger<LicenseStorageService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _licensePath = VmsRuntimePaths.GetLicensePath();
    }

    public bool LicenseFileExists() => File.Exists(_licensePath);

    public async Task<LicensePayload?> LoadLicenseAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_licensePath))
            return null;

        try
        {
            var protectedBytes = await File.ReadAllBytesAsync(_licensePath, ct);
            var jsonBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.LocalMachine);
            var json = Encoding.UTF8.GetString(jsonBytes);
            return JsonSerializer.Deserialize<LicensePayload>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load license file from {Path}", _licensePath);
            return null;
        }
    }

    public async Task StoreLicenseAsync(LicensePayload payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var protectedBytes = ProtectedData.Protect(jsonBytes, null, DataProtectionScope.LocalMachine);

        Directory.CreateDirectory(Path.GetDirectoryName(_licensePath)!);
        await File.WriteAllBytesAsync(_licensePath, protectedBytes, ct);

        await WriteRegistryChecksumAsync(payload);
        _logger.LogInformation("License stored for {Email} (ID: {LicenseId})", payload.CustomerEmail, payload.LicenseId);
    }

    public Task ClearLicenseAsync(CancellationToken ct = default)
    {
        if (File.Exists(_licensePath))
            File.Delete(_licensePath);

        try
        {
            Registry.LocalMachine.DeleteSubKeyTree(RegistryKeyPath, throwOnMissingSubKey: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear Registry license key");
        }

        return Task.CompletedTask;
    }

    public Task WriteRegistryChecksumAsync(LicensePayload payload)
    {
        try
        {
            var machineKey = DeriveMachineKey();
            var encLicenseId = EncryptValue(payload.LicenseId, machineKey);
            var checksum = ComputeChecksum(payload, machineKey);
            var encUpdatedAt = EncryptValue(DateTime.UtcNow.ToString("O"), machineKey);

            using var key = Registry.LocalMachine.CreateSubKey(RegistryKeyPath, writable: true);
            key.SetValue(RegValueLicenseId, encLicenseId);
            key.SetValue(RegValueChecksum, checksum);
            key.SetValue(RegValueUpdatedAt, encUpdatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write Registry license checksum — Registry may require elevated permissions");
        }
        return Task.CompletedTask;
    }

    public Task<bool> VerifyRegistryIntegrityAsync(LicensePayload payload)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryKeyPath);
            if (key == null)
            {
                _logger.LogWarning("Registry license key not found at {Path}", RegistryKeyPath);
                return Task.FromResult(false);
            }

            var storedChecksum = key.GetValue(RegValueChecksum)?.ToString();
            if (string.IsNullOrEmpty(storedChecksum))
                return Task.FromResult(false);

            var machineKey = DeriveMachineKey();
            var expectedChecksum = ComputeChecksum(payload, machineKey);

            bool valid = SlowEquals(storedChecksum, expectedChecksum);
            if (!valid)
                _logger.LogWarning("Registry checksum mismatch — license may have been tampered");

            return Task.FromResult(valid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Registry license key");
            return Task.FromResult(false);
        }
    }

    public async Task UpdateDatabaseRecordAsync(
        LicensePayload payload, bool[] componentScores,
        string? failureReason = null, CancellationToken ct = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var existing = db.LicenseRecords
                .FirstOrDefault(r => r.LicenseId == payload.LicenseId);

            if (existing == null)
            {
                var lt = Enum.TryParse<LicenseType>(payload.LicenseType, true, out var parsedType)
                    ? parsedType : LicenseType.Full;

                existing = new LicenseRecord
                {
                    LicenseId = payload.LicenseId,
                    LicenseType = lt,
                    CustomerEmail = payload.CustomerEmail,
                    IssuedAt = payload.IssuedAt,
                    ExpiresAt = payload.ExpiresAt,
                    Status = failureReason == null ? LicenseStatus.Valid : LicenseStatus.Invalid,
                    LastValidatedAt = DateTime.UtcNow,
                    ValidationCount = 1,
                    ComponentScoresJson = JsonSerializer.Serialize(componentScores),
                    ComponentMatchCount = componentScores.Count(s => s),
                    FailureReason = failureReason
                };
                db.LicenseRecords.Add(existing);
            }
            else
            {
                existing.LastValidatedAt = DateTime.UtcNow;
                existing.ValidationCount++;
                existing.ComponentScoresJson = JsonSerializer.Serialize(componentScores);
                existing.ComponentMatchCount = componentScores.Count(s => s);
                existing.Status = failureReason == null ? LicenseStatus.Valid : LicenseStatus.Invalid;
                existing.FailureReason = failureReason;
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update license database record");
        }
    }

    public async Task<bool> IsDatabaseRevokedAsync(string licenseId, CancellationToken ct = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return db.LicenseRecords.Any(r => r.LicenseId == licenseId && r.IsRevoked);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check database revocation status");
            return false;
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static byte[] DeriveMachineKey()
    {
        // Derives a machine-specific 256-bit key from stable Windows identifiers.
        // This key is used to encrypt Registry values — making them useless on other machines.
        var machineGuid = ReadRegistryString(
            @"SOFTWARE\Microsoft\Cryptography", "MachineGuid") ?? string.Empty;
        var productId = ReadRegistryString(
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductId") ?? string.Empty;

        var input = Encoding.UTF8.GetBytes(machineGuid + "|" + productId);
        var salt = Encoding.UTF8.GetBytes(KdfSalt);

        using var kdf = new Rfc2898DeriveBytes(input, salt, 100_000, HashAlgorithmName.SHA256);
        return kdf.GetBytes(32);
    }

    private static string EncryptValue(string plaintext, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        return Convert.ToBase64String(result);
    }

    private static string ComputeChecksum(LicensePayload payload, byte[] machineKey)
    {
        var data = $"{payload.LicenseId}|{payload.IssuedAt:O}|{payload.MachineFingerprint.FullHash}";
        using var hmac = new HMACSHA256(machineKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    private static bool SlowEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }

    private static string? ReadRegistryString(string subKey, string valueName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKey);
            return key?.GetValue(valueName)?.ToString();
        }
        catch { return null; }
    }
}
