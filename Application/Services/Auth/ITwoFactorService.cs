namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Interface for two-factor authentication operations
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Generates a two-factor authentication code for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated code</returns>
    Task<string> GenerateCodeAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a two-factor authentication code
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="code">Code to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if code is valid</returns>
    Task<bool> ValidateCodeAsync(int userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables two-factor authentication for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Setup key/QR code data</returns>
    Task<string> EnableTwoFactorAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables two-factor authentication for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task DisableTwoFactorAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if two-factor authentication is enabled for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if enabled</returns>
    Task<bool> IsTwoFactorEnabledAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates recovery codes for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recovery codes</returns>
    Task<List<string>> GenerateRecoveryCodesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a recovery code
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="recoveryCode">Recovery code to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if code is valid</returns>
    Task<bool> ValidateRecoveryCodeAsync(int userId, string recoveryCode, CancellationToken cancellationToken = default);
}
