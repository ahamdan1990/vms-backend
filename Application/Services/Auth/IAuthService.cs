using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Authentication service interface for login, logout, and token management
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates user with email and password
    /// </summary>
    /// <param name="loginRequest">Login request with credentials</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with tokens</returns>
    Task<AuthenticationResult> LoginAsync(LoginRequestDto loginRequest, string? ipAddress = null,
        string? userAgent = null, string? deviceFingerprint = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes access token using refresh token
    /// </summary>
    /// <param name="refreshTokenRequest">Refresh token request</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="userAgent">Client user agent</param>
    /// <param name="deviceFingerprint">Device fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New authentication tokens</returns>
    Task<AuthenticationResult> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenRequest, string? ipAddress = null,
        string? userAgent = null, string? deviceFingerprint = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out user and revokes refresh token
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="refreshToken">Refresh token to revoke</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout result</returns>
    Task<LogoutResult> LogoutAsync(int userId, string? refreshToken = null, string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out user from all devices
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="reason">Reason for logout</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout result</returns>
    Task<LogoutResult> LogoutFromAllDevicesAsync(int userId, string reason = "User logout",
        string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes user password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="changePasswordRequest">Password change request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password change result</returns>
    Task<PasswordChangeResult> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates password reset process
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset result</returns>
    Task<PasswordResetResultDto> InitiatePasswordResetAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets password using reset token
    /// </summary>
    /// <param name="resetPasswordRequest">Password reset request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset result</returns>
    Task<PasswordResetResultDto> ResetPasswordAsync(ResetPasswordDto resetPasswordRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates access token
    /// </summary>
    /// <param name="accessToken">Access token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token validation result</returns>
    Task<TokenValidationResult> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current user information from token
    /// </summary>
    /// <param name="accessToken">Access token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current user information</returns>
    Task<CurrentUserDto?> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates user credentials without creating session
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if credentials are valid</returns>
    Task<bool> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user account is locked out
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lockout status</returns>
    Task<UserLockoutStatus> GetLockoutStatusAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="unlockedBy">ID of user performing unlock</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unlocked successfully</returns>
    Task<bool> UnlockAccountAsync(int userId, int unlockedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user sessions information
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sessions</returns>
    Task<List<UserSessionDto>> GetUserSessionsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminates specific user session
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="sessionId">Session ID to terminate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if session terminated</returns>
    Task<bool> TerminateSessionAsync(int userId, string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates secure cookie options for authentication
    /// </summary>
    /// <param name="isSecure">Whether to use secure flag</param>
    /// <param name="sameSite">SameSite policy</param>
    /// <returns>Cookie options</returns>
    CookieOptions GetSecureCookieOptions(bool isSecure = true, SameSiteMode sameSite = SameSiteMode.Strict);

    /// <summary>
    /// Creates authentication cookies for response
    /// </summary>
    /// <param name="response">HTTP response</param>
    /// <param name="authResult">Authentication result</param>
    /// <param name="isSecure">Whether to use secure cookies</param>
    void SetAuthenticationCookies(HttpResponse response, AuthenticationResult authResult, bool isSecure = true);

    /// <summary>
    /// Clears authentication cookies from response
    /// </summary>
    /// <param name="response">HTTP response</param>
    void ClearAuthenticationCookies(HttpResponse response, bool isSecure = false);

    /// <summary>
    /// Extracts tokens from request cookies
    /// </summary>
    /// <param name="request">HTTP request</param>
    /// <returns>Token information from cookies</returns>
    CookieTokenInfo? ExtractTokensFromCookies(HttpRequest request);
}



