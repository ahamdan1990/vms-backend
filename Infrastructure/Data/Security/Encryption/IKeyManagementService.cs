namespace VisitorManagementSystem.Api.Infrastructure.Security.Encryption;

/// <summary>
/// Interface for encryption key management
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// Gets the current active encryption key
    /// </summary>
    /// <returns>Current encryption key</returns>
    Task<string> GetCurrentKeyAsync();

    /// <summary>
    /// Rotates the encryption key
    /// </summary>
    /// <returns>New encryption key</returns>
    Task<string> RotateKeyAsync();

    /// <summary>
    /// Gets a key by its ID
    /// </summary>
    /// <param name="keyId">Key identifier</param>
    /// <returns>Encryption key</returns>
    Task<string?> GetKeyByIdAsync(string keyId);

    /// <summary>
    /// Gets all active keys
    /// </summary>
    /// <returns>List of active keys with their IDs</returns>
    Task<Dictionary<string, string>> GetActiveKeysAsync();

    /// <summary>
    /// Deactivates a key
    /// </summary>
    /// <param name="keyId">Key identifier</param>
    /// <returns>True if successful</returns>
    Task<bool> DeactivateKeyAsync(string keyId);

    /// <summary>
    /// Validates key integrity
    /// </summary>
    /// <param name="keyId">Key identifier</param>
    /// <returns>True if key is valid</returns>
    Task<bool> ValidateKeyAsync(string keyId);

    /// <summary>
    /// Gets key rotation schedule
    /// </summary>
    /// <returns>Next rotation date</returns>
    Task<DateTime?> GetNextRotationDateAsync();

    /// <summary>
    /// Forces immediate key rotation
    /// </summary>
    /// <param name="reason">Reason for rotation</param>
    /// <returns>New key ID</returns>
    Task<string> ForceRotationAsync(string reason);
}