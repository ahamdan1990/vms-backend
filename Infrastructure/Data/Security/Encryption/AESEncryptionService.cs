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
            aes.Key = DeriveKeyFromPassword(key, aes.KeySize / 8);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using var swEncrypt = new StreamWriter(csEncrypt);

            swEncrypt.Write(plainText);
            swEncrypt.Close();

            var iv = aes.IV;
            var encryptedContent = msEncrypt.ToArray();
            var result = new byte[iv.Length + encryptedContent.Length];

            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encryptedContent, 0, result, iv.Length, encryptedContent.Length);

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
            var iv = new byte[ivLength];
            var cipher = new byte[fullCipher.Length - ivLength];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, ivLength);
            Buffer.BlockCopy(fullCipher, ivLength, cipher, 0, cipher.Length);

            aes.Key = DeriveKeyFromPassword(key, aes.KeySize / 8);
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

    private static byte[] DeriveKeyFromPassword(string password, int keyLength)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("VMS_SALT_2024"), 10000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keyLength);
    }
}