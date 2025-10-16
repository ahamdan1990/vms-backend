using System.Security.Cryptography;
using System.Text;

namespace VisitorManagementSystem.Api.Infrastructure.Security.Encryption;

/// <summary>
/// AES encryption service implementation
/// </summary>
public class AESEncryptionService : IEncryptionService
{
    private readonly string _defaultKey;
    private readonly ILogger<AESEncryptionService> _logger;

    public AESEncryptionService(IConfiguration configuration, ILogger<AESEncryptionService> logger)
    {
        _defaultKey = configuration["Security:EncryptionKeys:DatabaseEncryptionKey"]
                     ?? throw new InvalidOperationException("Encryption key not configured");
        _logger = logger;
    }

    public string Encrypt(string plainText)
    {
        return EncryptWithKey(plainText, _defaultKey);
    }

    public string Decrypt(string cipherText)
    {
        return DecryptWithKey(cipherText, _defaultKey);
    }

    public string EncryptWithKey(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));

        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            using var aes = Aes.Create();

            // Use new secure key derivation with unique salt
            var (derivedKey, salt) = DeriveKeyFromPasswordWithSalt(key, aes.KeySize / 8);
            aes.Key = derivedKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);

            swEncrypt.Write(plainText);
            swEncrypt.Close();

            var iv = aes.IV;
            var encryptedContent = msEncrypt.ToArray();

            // Format: [salt(32 bytes)][iv(16 bytes)][encrypted data]
            var result = new byte[salt.Length + iv.Length + encryptedContent.Length];

            Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
            Buffer.BlockCopy(iv, 0, result, salt.Length, iv.Length);
            Buffer.BlockCopy(encryptedContent, 0, result, salt.Length + iv.Length, encryptedContent.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during encryption");
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    public string DecryptWithKey(string cipherText, string key)
    {
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentException("Cipher text cannot be null or empty", nameof(cipherText));

        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            var ivLength = aes.BlockSize / 8;
            const int saltLength = 32; // 256-bit salt used in new format

            byte[] derivedKey;
            byte[] iv;
            byte[] cipher;

            // Detect format: new format has salt(32) + iv(16) + data, old format has iv(16) + data
            if (fullCipher.Length > saltLength + ivLength)
            {
                // Try new format first (salt + iv + encrypted data)
                try
                {
                    var salt = new byte[saltLength];
                    iv = new byte[ivLength];
                    cipher = new byte[fullCipher.Length - saltLength - ivLength];

                    Buffer.BlockCopy(fullCipher, 0, salt, 0, saltLength);
                    Buffer.BlockCopy(fullCipher, saltLength, iv, 0, ivLength);
                    Buffer.BlockCopy(fullCipher, saltLength + ivLength, cipher, 0, cipher.Length);

                    // Derive key using the extracted salt and new iteration count
                    var (key_derived, _) = DeriveKeyFromPasswordWithSalt(key, aes.KeySize / 8, salt);
                    derivedKey = key_derived;
                }
                catch
                {
                    // Fall back to old format
                    _logger.LogWarning("Failed to decrypt with new format, falling back to legacy format");
                    iv = new byte[ivLength];
                    cipher = new byte[fullCipher.Length - ivLength];

                    Buffer.BlockCopy(fullCipher, 0, iv, 0, ivLength);
                    Buffer.BlockCopy(fullCipher, ivLength, cipher, 0, cipher.Length);

#pragma warning disable CS0618 // Suppress obsolete warning for backward compatibility
                    derivedKey = DeriveKeyFromPassword(key, aes.KeySize / 8);
#pragma warning restore CS0618
                }
            }
            else
            {
                // Old format (no salt prefix)
                iv = new byte[ivLength];
                cipher = new byte[fullCipher.Length - ivLength];

                Buffer.BlockCopy(fullCipher, 0, iv, 0, ivLength);
                Buffer.BlockCopy(fullCipher, ivLength, cipher, 0, cipher.Length);

#pragma warning disable CS0618 // Suppress obsolete warning for backward compatibility
                derivedKey = DeriveKeyFromPassword(key, aes.KeySize / 8);
#pragma warning restore CS0618

                _logger.LogWarning("Decrypting data using legacy encryption format. Consider re-encrypting with new format.");
            }

            aes.Key = derivedKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(cipher);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during decryption");
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }

    public string GenerateKey(int keySize = 256)
    {
        var keyBytes = new byte[keySize / 8];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    public string GenerateIV()
    {
        using var aes = Aes.Create();
        aes.GenerateIV();
        return Convert.ToBase64String(aes.IV);
    }

    public string Hash(string data)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashedBytes).ToLowerInvariant();
    }

    public string HashWithSalt(string data, string salt)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        if (string.IsNullOrEmpty(salt))
            throw new ArgumentException("Salt cannot be null or empty", nameof(salt));

        var saltedData = data + salt;
        return Hash(saltedData);
    }

    public bool VerifyHash(string data, string hash, string salt)
    {
        var computedHash = HashWithSalt(data, salt);
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Derives a cryptographic key from a password using PBKDF2 with a unique salt
    /// </summary>
    /// <param name="password">Password to derive key from</param>
    /// <param name="keyLength">Desired key length in bytes</param>
    /// <param name="salt">Unique salt for this derivation (if null, generates new salt)</param>
    /// <returns>Tuple containing derived key bytes and the salt used</returns>
    private static (byte[] key, byte[] salt) DeriveKeyFromPasswordWithSalt(string password, int keyLength, byte[]? salt = null)
    {
        // Generate a cryptographically secure random salt if not provided
        if (salt == null)
        {
            salt = new byte[32]; // 256-bit salt
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
        }

        // Use OWASP 2023 recommended iteration count for PBKDF2-SHA256 (600,000+)
        const int iterations = 600000;

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(keyLength);

        return (key, salt);
    }

    /// <summary>
    /// Legacy method - kept for backward compatibility but should not be used for new encryption
    /// </summary>
    [Obsolete("This method uses a fixed salt and is insecure. Use the new encryption methods instead.")]
    private static byte[] DeriveKeyFromPassword(string password, int keyLength)
    {
        // Kept for decrypting existing data encrypted with old method
        // WARNING: This is insecure - uses hardcoded salt
        using var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("VMS_SALT_2024"), 10000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keyLength);
    }
}