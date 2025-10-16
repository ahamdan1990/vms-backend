namespace VisitorManagementSystem.Api.Infrastructure.Security.Encryption;

/// <summary>
/// Interface for encryption and decryption services
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts data using AES encryption
    /// </summary>
    /// <param name="plainText">Data to encrypt</param>
    /// <returns>Encrypted data as base64 string</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts data using AES encryption
    /// </summary>
    /// <param name="cipherText">Encrypted data as base64 string</param>
    /// <returns>Decrypted plain text</returns>
    string Decrypt(string cipherText);

    /// <summary>
    /// Encrypts data with a specific key
    /// </summary>
    /// <param name="plainText">Data to encrypt</param>
    /// <param name="key">Encryption key</param>
    /// <returns>Encrypted data</returns>
    string EncryptWithKey(string plainText, string key);

    /// <summary>
    /// Decrypts data with a specific key
    /// </summary>
    /// <param name="cipherText">Encrypted data</param>
    /// <param name="key">Decryption key</param>
    /// <returns>Decrypted data</returns>
    string DecryptWithKey(string cipherText, string key);

    /// <summary>
    /// Generates a secure random key
    /// </summary>
    /// <param name="keySize">Key size in bits</param>
    /// <returns>Base64 encoded key</returns>
    string GenerateKey(int keySize = 256);

    /// <summary>
    /// Generates a secure random IV
    /// </summary>
    /// <returns>Base64 encoded IV</returns>
    string GenerateIV();

    /// <summary>
    /// Hashes data using SHA-256
    /// </summary>
    /// <param name="data">Data to hash</param>
    /// <returns>Hash as hex string</returns>
    string Hash(string data);

    /// <summary>
    /// Hashes data with salt using SHA-256
    /// </summary>
    /// <param name="data">Data to hash</param>
    /// <param name="salt">Salt value</param>
    /// <returns>Hash as hex string</returns>
    string HashWithSalt(string data, string salt);

    /// <summary>
    /// Verifies a hash against original data
    /// </summary>
    /// <param name="data">Original data</param>
    /// <param name="hash">Hash to verify</param>
    /// <param name="salt">Salt used in hashing</param>
    /// <returns>True if hash matches</returns>
    bool VerifyHash(string data, string hash, string salt);
}