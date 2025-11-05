using System.Security.Cryptography;
using System.Text;

namespace VisitorManagementSystem.Api.Infrastructure.Utilities;

/// <summary>
/// Cryptographic utility helper for password hashing, token generation, and security operations
/// </summary>
public static class CryptoHelper
{
    private const int DefaultSaltSize = 32;
    private const int DefaultHashSize = 32;
    private const int DefaultIterations = 100000;
    private const int DefaultTokenLength = 32;

    /// <summary>
    /// Generates a cryptographically secure random salt
    /// </summary>
    /// <param name="size">Salt size in bytes</param>
    /// <returns>Base64 encoded salt</returns>
    public static string GenerateSalt(int size = DefaultSaltSize)
    {
        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[size];
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    /// <summary>
    /// Hashes a password using PBKDF2 with SHA-256
    /// </summary>
    /// <param name="password">Password to hash</param>
    /// <param name="salt">Salt for hashing</param>
    /// <param name="iterations">Number of iterations</param>
    /// <param name="hashSize">Hash size in bytes</param>
    /// <returns>Base64 encoded hash</returns>
    public static string HashPassword(string password, string salt, int iterations = DefaultIterations, int hashSize = DefaultHashSize)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        if (string.IsNullOrEmpty(salt))
            throw new ArgumentException("Salt cannot be null or empty", nameof(salt));

        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256);
        var hashBytes = pbkdf2.GetBytes(hashSize);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    /// <param name="password">Password to verify</param>
    /// <param name="hash">Stored hash</param>
    /// <param name="salt">Salt used for hashing</param>
    /// <param name="iterations">Number of iterations used</param>
    /// <param name="hashSize">Hash size in bytes</param>
    /// <returns>True if password matches</returns>
    public static bool VerifyPassword(string password, string hash, string salt, int iterations = DefaultIterations, int hashSize = DefaultHashSize)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
            return false;

        try
        {
            var computedHash = HashPassword(password, salt, iterations, hashSize);
            return SlowEquals(hash, computedHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a complete password hash with embedded salt and parameters
    /// </summary>
    /// <param name="password">Password to hash</param>
    /// <param name="iterations">Number of iterations</param>
    /// <returns>Complete hash string with metadata</returns>
    public static string GeneratePasswordHash(string password, int iterations = DefaultIterations)
    {
        var salt = GenerateSalt();
        var hash = HashPassword(password, salt, iterations);
        return $"$pbkdf2-sha256${iterations}${salt}${hash}";
    }

    /// <summary>
    /// Verifies a password against a complete hash
    /// </summary>
    /// <param name="password">Password to verify</param>
    /// <param name="completeHash">Complete hash with metadata</param>
    /// <returns>True if password matches</returns>
    public static bool VerifyPasswordHash(string password, string completeHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(completeHash))
            return false;

        var parts = completeHash.Split('$');
        if (parts.Length != 5 || parts[0] != "" || parts[1] != "pbkdf2-sha256")
            return false;

        if (!int.TryParse(parts[2], out var iterations))
            return false;

        var salt = parts[3];
        var hash = parts[4];

        return VerifyPassword(password, hash, salt, iterations);
    }

    /// <summary>
    /// Generates a cryptographically secure random token
    /// </summary>
    /// <param name="length">Token length in bytes</param>
    /// <returns>Base64 encoded token</returns>
    public static string GenerateSecureToken(int length = DefaultTokenLength)
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[length];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    /// <summary>
    /// Generates a URL-safe secure token
    /// </summary>
    /// <param name="length">Token length in bytes</param>
    /// <returns>URL-safe token</returns>
    public static string GenerateUrlSafeToken(int length = DefaultTokenLength)
    {
        var token = GenerateSecureToken(length);
        return token.Replace("/", "_").Replace("+", "-").TrimEnd('=');
    }

    /// <summary>
    /// Generates a secure numeric token
    /// </summary>
    /// <param name="length">Number of digits</param>
    /// <returns>Numeric token</returns>
    public static string GenerateNumericToken(int length = 6)
    {
        using var rng = RandomNumberGenerator.Create();
        var result = new StringBuilder();

        for (int i = 0; i < length; i++)
        {
            var randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            var randomInt = Math.Abs(BitConverter.ToInt32(randomBytes, 0));
            result.Append((randomInt % 10).ToString());
        }

        return result.ToString();
    }

    /// <summary>
    /// Computes SHA-256 hash of input
    /// </summary>
    /// <param name="input">Input string</param>
    /// <returns>Hex-encoded hash</returns>
    public static string ComputeSha256Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Computes SHA-512 hash of input
    /// </summary>
    /// <param name="input">Input string</param>
    /// <returns>Hex-encoded hash</returns>
    public static string ComputeSha512Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using var sha512 = SHA512.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha512.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Computes HMAC-SHA256 of input with key
    /// </summary>
    /// <param name="input">Input string</param>
    /// <param name="key">HMAC key</param>
    /// <returns>Hex-encoded HMAC</returns>
    public static string ComputeHmacSha256(string input, string key)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(key))
            return string.Empty;

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(input);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies HMAC signature
    /// </summary>
    /// <param name="input">Input string</param>
    /// <param name="key">HMAC key</param>
    /// <param name="signature">Signature to verify</param>
    /// <returns>True if signature is valid</returns>
    public static bool VerifyHmacSignature(string input, string key, string signature)
    {
        var computedSignature = ComputeHmacSha256(input, key);
        return SlowEquals(signature, computedSignature);
    }

    /// <summary>
    /// Generates a secure API key
    /// </summary>
    /// <param name="prefix">Optional prefix</param>
    /// <returns>Secure API key</returns>
    public static string GenerateApiKey(string prefix = "vms")
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString("x");
        var randomPart = GenerateUrlSafeToken(32);
        return $"{prefix}_{timestamp}_{randomPart}";
    }

    /// <summary>
    /// Generates a webhook signature
    /// </summary>
    /// <param name="payload">Webhook payload</param>
    /// <param name="secret">Webhook secret</param>
    /// <returns>Webhook signature</returns>
    public static string GenerateWebhookSignature(string payload, string secret)
    {
        var signature = ComputeHmacSha256(payload, secret);
        return $"sha256={signature}";
    }

    /// <summary>
    /// Verifies a webhook signature
    /// </summary>
    /// <param name="payload">Webhook payload</param>
    /// <param name="secret">Webhook secret</param>
    /// <param name="signature">Signature to verify</param>
    /// <returns>True if signature is valid</returns>
    public static bool VerifyWebhookSignature(string payload, string secret, string signature)
    {
        if (string.IsNullOrEmpty(signature) || !signature.StartsWith("sha256="))
            return false;

        var expectedSignature = GenerateWebhookSignature(payload, secret);
        return SlowEquals(signature, expectedSignature);
    }

    /// <summary>
    /// Encrypts data using AES-256-GCM
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <param name="key">Encryption key</param>
    /// <returns>Encrypted data with nonce and tag</returns>
    public static string EncryptAesGcm(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var keyBytes = Convert.FromBase64String(key);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[12]; // 96-bit nonce for GCM
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16]; // 128-bit authentication tag

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(nonce);

        using var aes = new AesGcm(keyBytes, 16); // 16-byte (128-bit) tag
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Combine nonce + tag + cipher
        var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Array.Copy(nonce, 0, result, 0, nonce.Length);
        Array.Copy(tag, 0, result, nonce.Length, tag.Length);
        Array.Copy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts data encrypted with AES-256-GCM
    /// </summary>
    /// <param name="cipherText">Encrypted data</param>
    /// <param name="key">Decryption key</param>
    /// <returns>Decrypted plain text</returns>
    public static string DecryptAesGcm(string cipherText, string key)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        var keyBytes = Convert.FromBase64String(key);
        var encryptedBytes = Convert.FromBase64String(cipherText);

        if (encryptedBytes.Length < 28) // 12 (nonce) + 16 (tag) minimum
            throw new ArgumentException("Invalid encrypted data");

        var nonce = new byte[12];
        var tag = new byte[16];
        var cipherBytes = new byte[encryptedBytes.Length - 28];

        Array.Copy(encryptedBytes, 0, nonce, 0, 12);
        Array.Copy(encryptedBytes, 12, tag, 0, 16);
        Array.Copy(encryptedBytes, 28, cipherBytes, 0, cipherBytes.Length);

        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(keyBytes, 16); // 16-byte (128-bit) tag
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Generates a time-based one-time password (TOTP)
    /// </summary>
    /// <param name="secret">Shared secret</param>
    /// <param name="timeStep">Time step in seconds</param>
    /// <param name="digits">Number of digits</param>
    /// <returns>TOTP code</returns>
    public static string GenerateTotp(string secret, int timeStep = 30, int digits = 6)
    {
        var secretBytes = Convert.FromBase64String(secret);
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timeCounter = unixTime / timeStep;

        return GenerateHotp(secretBytes, timeCounter, digits);
    }

    /// <summary>
    /// Verifies a TOTP code
    /// </summary>
    /// <param name="code">Code to verify</param>
    /// <param name="secret">Shared secret</param>
    /// <param name="windowSize">Allowed time window</param>
    /// <param name="timeStep">Time step in seconds</param>
    /// <param name="digits">Number of digits</param>
    /// <returns>True if code is valid</returns>
    public static bool VerifyTotp(string code, string secret, int windowSize = 1, int timeStep = 30, int digits = 6)
    {
        var secretBytes = Convert.FromBase64String(secret);
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timeCounter = unixTime / timeStep;

        for (int i = -windowSize; i <= windowSize; i++)
        {
            var testCode = GenerateHotp(secretBytes, timeCounter + i, digits);
            if (SlowEquals(code, testCode))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Generates HMAC-based one-time password
    /// </summary>
    private static string GenerateHotp(byte[] secret, long counter, int digits)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[hash.Length - 1] & 0x0F;
        var truncated = ((hash[offset] & 0x7F) << 24) |
                       ((hash[offset + 1] & 0xFF) << 16) |
                       ((hash[offset + 2] & 0xFF) << 8) |
                       (hash[offset + 3] & 0xFF);

        var code = truncated % (int)Math.Pow(10, digits);
        return code.ToString().PadLeft(digits, '0');
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks
    /// </summary>
    /// <param name="a">First string</param>
    /// <param name="b">Second string</param>
    /// <returns>True if strings are equal</returns>
    public static bool SlowEquals(string a, string b)
    {
        if (a == null || b == null)
            return false;

        var diff = (uint)a.Length ^ (uint)b.Length;
        for (int i = 0; i < a.Length && i < b.Length; i++)
        {
            diff |= (uint)a[i] ^ (uint)b[i];
        }
        return diff == 0;
    }

    /// <summary>
    /// Generates a secure device fingerprint
    /// </summary>
    /// <param name="userAgent">User agent string</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="additionalData">Additional fingerprint data</param>
    /// <returns>Device fingerprint</returns>
    public static string GenerateDeviceFingerprint(string userAgent, string ipAddress, params string[] additionalData)
    {
        var data = new List<string> { userAgent ?? "", ipAddress ?? "" };
        data.AddRange(additionalData ?? Array.Empty<string>());

        var combined = string.Join("|", data);
        var hash = ComputeSha256Hash(combined);
        return $"fp_{hash.Substring(0, 16)}";
    }

    /// <summary>
    /// Validates password strength
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>Password strength score (0-100)</returns>
    public static int CalculatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return 0;

        var score = 0;

        // Length scoring
        if (password.Length >= 8) score += 10;
        if (password.Length >= 12) score += 10;
        if (password.Length >= 16) score += 10;

        // Character variety
        if (password.Any(char.IsLower)) score += 10;
        if (password.Any(char.IsUpper)) score += 10;
        if (password.Any(char.IsDigit)) score += 10;
        if (password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c))) score += 20;

        // Pattern penalties
        if (HasRepeatingChars(password)) score -= 10;
        if (HasSequentialChars(password)) score -= 10;
        if (IsCommonPassword(password)) score -= 20;

        return Math.Max(0, Math.Min(100, score));
    }

    private static bool HasRepeatingChars(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i] == password[i + 2])
                return true;
        }
        return false;
    }

    private static bool HasSequentialChars(string password)
    {
        var sequences = new[] { "abc", "bcd", "cde", "def", "123", "234", "345", "456", "789" };
        return sequences.Any(seq => password.ToLowerInvariant().Contains(seq));
    }

    private static bool IsCommonPassword(string password)
    {
        var common = new[] { "password", "123456", "qwerty", "admin", "letmein", "welcome" };
        return common.Any(p => password.ToLowerInvariant().Contains(p));
    }
}