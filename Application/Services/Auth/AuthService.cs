using Microsoft.Extensions.Options;

using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.Services.Users;
using VisitorManagementSystem.Api.Application.Services.Configuration;

using VisitorManagementSystem.Api.Domain.Constants;

using VisitorManagementSystem.Api.Domain.Entities;

using VisitorManagementSystem.Api.Domain.Enums;

using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

using VisitorManagementSystem.Api.Infrastructure.Utilities;



namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Authentication service implementation with cookie-based token management
/// </summary>
public class AuthService : IAuthService

{

    private readonly IUnitOfWork _unitOfWork;

    private readonly IJwtService _jwtService;

    private readonly IPasswordService _passwordService;

    private readonly IPermissionService _permissionService;

    private readonly IUserLockoutService _lockoutService;

    private readonly IRefreshTokenService _refreshTokenService;

    private readonly ILogger<AuthService> _logger;

    private readonly IDynamicConfigurationService _dynamicConfig;

    private readonly IServiceProvider _serviceProvider;



    public AuthService(

        IUnitOfWork unitOfWork,

        IJwtService jwtService,

        IPasswordService passwordService,

        IPermissionService permissionService,

        IUserLockoutService lockoutService,

        IRefreshTokenService refreshTokenService,

        ILogger<AuthService> logger,

        IDynamicConfigurationService dynamicConfig,

        IServiceProvider serviceProvider)

    {

        _unitOfWork = unitOfWork;

        _jwtService = jwtService;

        _passwordService = passwordService;

        _permissionService = permissionService;

        _lockoutService = lockoutService;

        _refreshTokenService = refreshTokenService;

        _logger = logger;

        _dynamicConfig = dynamicConfig;

        _serviceProvider = serviceProvider;

    }



    public async Task<AuthenticationResult> LoginAsync(LoginRequestDto loginRequest, string? ipAddress = null,

        string? userAgent = null, string? deviceFingerprint = null, CancellationToken cancellationToken = default)

    {

        try

        {

            _logger.LogInformation("Login attempt for user: {Email} from IP: {IpAddress}",

                loginRequest.Email, ipAddress);



            // Check lockout status first
            var lockoutStatus = await _lockoutService.GetLockoutStatusAsync(loginRequest.Email, cancellationToken);
            if (lockoutStatus.IsLockedOut)

            {

                _logger.LogWarning("Login attempt for locked user: {Email}", loginRequest.Email);

                return new AuthenticationResult

                {

                    IsSuccess = false,

                    ErrorMessage = "Account is locked due to multiple failed login attempts",

                    LockoutTimeRemaining = lockoutStatus.TimeRemaining

                };

            }

            // Get user by email
            var user = await _unitOfWork.Users.GetByEmailAsync(loginRequest.Email, cancellationToken);
            if (user == null)

            {

                await _lockoutService.RecordFailedLoginAttemptAsync(loginRequest.Email, ipAddress,

                    userAgent, "User not found", cancellationToken);



                _logger.LogWarning("Login attempt with non-existent email: {Email}", loginRequest.Email);

                return new AuthenticationResult

                {

                    IsSuccess = false,

                    ErrorMessage = "Invalid email or password",

                    Errors = { ValidationMessages.User.InvalidCredentials }

                };

            }

            // Validate user status
            if (!user.IsValidForAuthentication())

            {

                await _lockoutService.RecordFailedLoginAttemptAsync(loginRequest.Email, ipAddress,

                    userAgent, $"Account status: {user.Status}", cancellationToken);



                _logger.LogWarning("Login attempt for invalid user account: {Email}, Status: {Status}",

                    loginRequest.Email, user.Status);



                return new AuthenticationResult

                {

                    IsSuccess = false,

                    ErrorMessage = GetStatusErrorMessage(user.Status),

                    Errors = { GetStatusErrorMessage(user.Status) }

                };

            }

            // Verify password
            if (!_passwordService.VerifyPassword(loginRequest.Password, user.PasswordHash, user.PasswordSalt))

            {

                var lockoutResult = await _lockoutService.RecordFailedLoginAttemptAsync(

                    loginRequest.Email, ipAddress, userAgent, "Invalid password", cancellationToken);



                _logger.LogWarning("Failed login attempt for user: {Email}, Failed attempts: {FailedAttempts}",

                    loginRequest.Email, lockoutResult.FailedAttempts);



                var result = new AuthenticationResult

                {

                    IsSuccess = false,

                    ErrorMessage = "Invalid email or password",

                    Errors = { ValidationMessages.User.InvalidCredentials }

                };



                if (lockoutResult.IsLockedOut)

                {

                    result.ErrorMessage = "Account locked due to multiple failed login attempts";

                    result.LockoutTimeRemaining = lockoutResult.LockoutDuration;

                }



                return result;

            }



            // Check if password is expired or needs change

            var requiresPasswordChange = user.MustChangePassword || _passwordService.IsPasswordExpired(user);



            // Generate device fingerprint if not provided

            deviceFingerprint ??= CryptoHelper.GenerateDeviceFingerprint(userAgent ?? "", ipAddress ?? "");



            // Get user permissions

            var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);



            // Generate tokens

            var tokenResult = await _jwtService.GenerateAccessTokenAsync(user, permissions);

            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(

                user.Id, tokenResult.JwtId, deviceFingerprint, ipAddress, userAgent, cancellationToken);



            user.ResetFailedLoginAttempts();

            _unitOfWork.Users.Update(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);



            // Record successful login event (no database changes, just logging/cache)

            try

            {

                await _lockoutService.RecordSuccessfulLoginEventAsync(user.Id, ipAddress, userAgent, cancellationToken);

            }

            catch (Exception ex)

            {

                // Don't fail login if event recording fails

                _logger.LogWarning(ex, "Failed to record login event for user: {UserId}", user.Id);

            }



            _logger.LogInformation("Successful login for user: {Email} (ID: {UserId})",

                user.Email.Value, user.Id);



            return new AuthenticationResult

            {

                IsSuccess = true,

                AccessToken = tokenResult.Token,

                RefreshToken = refreshToken.Token,

                AccessTokenExpiry = tokenResult.Expiry,

                RefreshTokenExpiry = refreshToken.ExpiryDate,

                User = new CurrentUserDto

                {

                    Id = user.Id,

                    Email = user.Email.Value,

                    FirstName = user.FirstName,

                    LastName = user.LastName,

                    FullName = user.FullName,

                    Role = user.Role.ToString(),

                    Status = user.Status.ToString(),

                    Department = user.Department,

                    JobTitle = user.JobTitle,

                    TimeZone = user.TimeZone,

                    Language = user.Language,

                    Theme = user.Theme,

                    Permissions = permissions,

                    LastLoginDate = user.LastLoginDate,

                    PasswordChangedDate = user.PasswordChangedDate

                },

                RequiresPasswordChange = requiresPasswordChange

            };

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error during login for user: {Email}", loginRequest.Email);

            return new AuthenticationResult

            {

                IsSuccess = false,

                ErrorMessage = "An error occurred during login",

                Errors = { "Authentication failed due to system error" }

            };

        }

    }



    public async Task<AuthenticationResult> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenRequest,

        string? ipAddress = null, string? userAgent = null, string? deviceFingerprint = null,

        CancellationToken cancellationToken = default)

    {

        try

        {

            _logger.LogDebug("Refresh token request from IP: {IpAddress}", ipAddress);



            // Validate refresh token

            var tokenValidation = await _refreshTokenService.ValidateRefreshTokenAsync(

                refreshTokenRequest.RefreshToken, cancellationToken);



            if (!tokenValidation.IsValid || tokenValidation.Token == null || tokenValidation.User == null)

            {

                _logger.LogWarning("Invalid refresh token used from IP: {IpAddress}", ipAddress);

                return new AuthenticationResult

                {

                    IsSuccess = false,

                    ErrorMessage = tokenValidation.ErrorMessage ?? "Invalid refresh token",

                    Errors = tokenValidation.ValidationErrors

                };

            }



            var user = tokenValidation.User;



            // Validate user status

            if (!user.IsValidForAuthentication())

            {

                _logger.LogWarning("Refresh token used for invalid user account: {Email}, Status: {Status}",

                    user.Email.Value, user.Status);



                return new AuthenticationResult

                {

                    IsSuccess = false,

                    ErrorMessage = GetStatusErrorMessage(user.Status)

                };

            }



            // Get user permissions

            var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);



            // Generate new tokens

            var newTokenResult = await _jwtService.GenerateAccessTokenAsync(user, permissions);



            // Use the refresh token (creates new one and marks old as used)

            var refreshResult = await _refreshTokenService.UseRefreshTokenAsync(

                refreshTokenRequest.RefreshToken, newTokenResult.JwtId, ipAddress, userAgent,

                deviceFingerprint, cancellationToken);



            if (!refreshResult.IsSuccess || refreshResult.NewToken == null)

            {

                _logger.LogWarning("Failed to refresh token for user: {Email}", user.Email.Value);

                return new AuthenticationResult

                {

                    IsSuccess = false,

                    ErrorMessage = refreshResult.ErrorMessage ?? "Failed to refresh token",

                    Errors = refreshResult.Errors

                };

            }



            _logger.LogDebug("Successfully refreshed token for user: {Email} (ID: {UserId})",

                user.Email.Value, user.Id);



            return new AuthenticationResult

            {

                IsSuccess = true,

                AccessToken = newTokenResult.Token,

                RefreshToken = refreshResult.NewToken.Token,

                AccessTokenExpiry = newTokenResult.Expiry,

                RefreshTokenExpiry = refreshResult.NewToken.ExpiryDate,

                User = new CurrentUserDto

                {

                    Id = user.Id,

                    Email = user.Email.Value,

                    FirstName = user.FirstName,

                    LastName = user.LastName,

                    FullName = user.FullName,

                    Role = user.Role.ToString(),

                    Status = user.Status.ToString(),

                    Department = user.Department,

                    JobTitle = user.JobTitle,

                    TimeZone = user.TimeZone,

                    Language = user.Language,

                    Theme = user.Theme,

                    Permissions = permissions,

                    LastLoginDate = user.LastLoginDate,

                    PasswordChangedDate = user.PasswordChangedDate

                }

            };

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error during token refresh from IP: {IpAddress}", ipAddress);

            return new AuthenticationResult

            {

                IsSuccess = false,

                ErrorMessage = "An error occurred during token refresh",

                Errors = { "Token refresh failed due to system error" }

            };

        }

    }



    public async Task<LogoutResult> LogoutAsync(int userId, string? refreshToken = null,

        string? ipAddress = null, CancellationToken cancellationToken = default)

    {

        try

        {

            _logger.LogInformation("Logout request for user: {UserId} from IP: {IpAddress}", userId, ipAddress);



            int tokensRevoked = 0;



            if (!string.IsNullOrEmpty(refreshToken))

            {

                // Revoke specific refresh token

                var revoked = await _refreshTokenService.RevokeRefreshTokenAsync(

                    refreshToken, "User logout", ipAddress, cancellationToken);

                if (revoked) tokensRevoked = 1;

            }

            else

            {

                // Revoke all tokens for user

                tokensRevoked = await _refreshTokenService.RevokeAllUserTokensAsync(

                    userId, "User logout from all devices", ipAddress, cancellationToken);

            }



            _logger.LogInformation("Logout successful for user: {UserId}, Tokens revoked: {TokensRevoked}",

                userId, tokensRevoked);



            return new LogoutResult

            {

                IsSuccess = true,

                Message = "Logout successful",

                TokensRevoked = tokensRevoked

            };

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error during logout for user: {UserId}", userId);

            return new LogoutResult

            {

                IsSuccess = false,

                Message = "An error occurred during logout",

                TokensRevoked = 0

            };

        }

    }



    public async Task<LogoutResult> LogoutFromAllDevicesAsync(int userId, string reason = "User logout",

        string? ipAddress = null, CancellationToken cancellationToken = default)

    {

        try

        {

            _logger.LogInformation("Logout from all devices for user: {UserId}, Reason: {Reason}", userId, reason);



            var tokensRevoked = await _refreshTokenService.RevokeAllUserTokensAsync(

                userId, reason, ipAddress, cancellationToken);



            // Update user security stamp to invalidate all JWT tokens

            await _jwtService.RevokeAllUserTokensAsync(userId);



            _logger.LogInformation("Logout from all devices successful for user: {UserId}, Tokens revoked: {TokensRevoked}",

                userId, tokensRevoked);



            return new LogoutResult

            {

                IsSuccess = true,

                Message = "Successfully logged out from all devices",

                TokensRevoked = tokensRevoked

            };

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error during logout from all devices for user: {UserId}", userId);

            return new LogoutResult

            {

                IsSuccess = false,

                Message = "An error occurred during logout",

                TokensRevoked = 0

            };

        }

    }



    public async Task<PasswordChangeResult> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordRequest,

        CancellationToken cancellationToken = default)

    {

        try

        {

            _logger.LogInformation("Password change request for user: {UserId}", userId);



            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

            if (user == null)

            {

                return new PasswordChangeResult

                {

                    IsSuccess = false,

                    ErrorMessage = "User not found"

                };

            }



            // Validate password change

            var validation = await _passwordService.ValidatePasswordChangeAsync(

                user, changePasswordRequest.CurrentPassword, changePasswordRequest.NewPassword);



            if (!validation.IsValid)

            {

                return new PasswordChangeResult

                {

                    IsSuccess = false,

                    ErrorMessage = "Password change validation failed",

                    Errors = validation.Errors

                };

            }



            // Hash new password

            var hashResult = _passwordService.HashPassword(changePasswordRequest.NewPassword);



            // Save password history

            await _passwordService.SavePasswordHistoryAsync(userId, user.PasswordHash, user.PasswordSalt, cancellationToken);



            // Update user password

            user.ChangePassword(hashResult.Hash, hashResult.Salt);

            _unitOfWork.Users.Update(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);



            // Revoke all existing tokens to force re-authentication

            await LogoutFromAllDevicesAsync(userId, "Password changed", cancellationToken: cancellationToken);



            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);



            return new PasswordChangeResult

            {

                IsSuccess = true,

                RequiresReauthentication = true

            };

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error during password change for user: {UserId}", userId);

            return new PasswordChangeResult

            {

                IsSuccess = false,

                ErrorMessage = "An error occurred during password change"

            };

        }

    }



    public async Task<PasswordResetResultDto> InitiatePasswordResetAsync(string email, CancellationToken cancellationToken = default)

    {

        try

        {

            _logger.LogInformation("Password reset initiated for email: {Email}", email);



            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);

            if (user == null)

            {

                // Return success to prevent email enumeration

                _logger.LogWarning("Password reset attempted for non-existent email: {Email}", email);

                return new PasswordResetResultDto

                {

                    IsSuccess = true,

                    EmailSent = false

                };

            }



            if (!user.IsValidForAuthentication())

            {

                _logger.LogWarning("Password reset attempted for invalid user: {Email}, Status: {Status}",

                    email, user.Status);

                return new PasswordResetResultDto

                {

                    IsSuccess = false,

                    ErrorMessage = "Account is not eligible for password reset"

                };

            }



            // Generate reset token

            var resetToken = _passwordService.GeneratePasswordResetToken(user);

            var passwordResetTokenExpiryMinutes = await _dynamicConfig.GetConfigurationAsync<int>("JWT", "PasswordResetTokenExpiryMinutes", 30, cancellationToken);
            var expiryTime = DateTime.UtcNow.AddMinutes(passwordResetTokenExpiryMinutes);



            // Send password reset email
            try
            {
                var emailService = _serviceProvider.GetService<Application.Services.Email.IEmailService>();
                var emailTemplateService = _serviceProvider.GetService<Application.Services.Email.IEmailTemplateService>();
                
                if (emailService != null && emailTemplateService != null)
                {
                    var emailContent = await emailTemplateService.GeneratePasswordResetTemplateAsync(user, resetToken);
                    await emailService.SendAsync(user.Email, "Password Reset Request", emailContent);
                    
                    _logger.LogInformation("Password reset email sent to {Email}", user.Email);
                }
                else
                {
                    _logger.LogWarning("Email services not available. Password reset token generated but email not sent.");
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send password reset email to {Email}", user.Email);
                // Don't throw - the token was created successfully
            }



            _logger.LogInformation("Password reset token generated for user: {Email}", email);



            return new PasswordResetResultDto

            {

                IsSuccess = true,

                ResetToken = resetToken, // Remove this in production - should only be sent via email

                ResetTokenExpiry = expiryTime,

                EmailSent = true

            };

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error during password reset initiation for email: {Email}", email);

            return new PasswordResetResultDto

            {

                IsSuccess = false,

                ErrorMessage = "An error occurred during password reset initiation"

            };

        }

    }



    public async Task<PasswordResetResultDto> ResetPasswordAsync(ResetPasswordDto resetPasswordRequest,

        CancellationToken cancellationToken = default)

    {

        try

        {

            _logger.LogInformation("Password reset with token for email: {Email}", resetPasswordRequest.Email);



            var user = await _unitOfWork.Users.GetByEmailAsync(resetPasswordRequest.Email, cancellationToken);

            if (user == null)

            {

                return new PasswordResetResultDto

                {

                    IsSuccess = false,

                    ErrorMessage = "Invalid reset request"

                };

            }



            // Validate reset token

            if (!_passwordService.ValidatePasswordResetToken(user, resetPasswordRequest.Token))

            {

                _logger.LogWarning("Invalid password reset token used for email: {Email}", resetPasswordRequest.Email);

                return new PasswordResetResultDto

                {

                    IsSuccess = false,

                    ErrorMessage = "Invalid or expired reset token"

                };

            }



            // Validate new password

            var passwordValidation = _passwordService.ValidatePassword(resetPasswordRequest.NewPassword, user);

            if (!passwordValidation.IsValid)

            {

                return new PasswordResetResultDto

                {

                    IsSuccess = false,

                    ErrorMessage = "New password does not meet requirements",

                    Errors = passwordValidation.Errors

                };

            }



            // Hash new password

            var hashResult = _passwordService.HashPassword(resetPasswordRequest.NewPassword);



            // Save current password to history

            await _passwordService.SavePasswordHistoryAsync(user.Id, user.PasswordHash, user.PasswordSalt, cancellationToken);



            // Update password

            user.ChangePassword(hashResult.Hash, hashResult.Salt);

            _unitOfWork.Users.Update(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);



            // Revoke all tokens

            await LogoutFromAllDevicesAsync(user.Id, "Password reset", cancellationToken: cancellationToken);



            _logger.LogInformation("Password reset successfully for user: {Email}", resetPasswordRequest.Email);



            return new PasswordResetResultDto

            {

                IsSuccess = true

            };

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error during password reset for email: {Email}", resetPasswordRequest.Email);

            return new PasswordResetResultDto

            {

                IsSuccess = false,

                ErrorMessage = "An error occurred during password reset"

            };

        }

    }



    public async Task<TokenValidationResult> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)

    {

        try

        {

            var validationResult = await _jwtService.ValidateTokenAsync(accessToken);

            var now = DateTime.UtcNow;
            bool isExpired = validationResult.Expiry.HasValue && validationResult.Expiry.Value <= now;
            bool isNearExpiry = validationResult.Expiry.HasValue && validationResult.Expiry.Value <= now.AddMinutes(2);

            if (!validationResult.IsValid)

            {

                return new TokenValidationResult

                {

                    IsValid = false,

                    ErrorMessage = validationResult.ErrorMessage

                };

            }



            // Additional validation against user status and security stamp

            if (validationResult.UserId.HasValue)

            {

                var user = await _unitOfWork.Users.GetByIdAsync(validationResult.UserId.Value, cancellationToken);

                if (user == null || !user.IsValidForAuthentication())

                {

                    return new TokenValidationResult

                    {

                        IsValid = false,

                        ErrorMessage = "User account is no longer valid"

                    };

                }



                // Validate security stamp

                if (validationResult.SecurityStamp != user.SecurityStamp)

                {

                    return new TokenValidationResult

                    {

                        IsValid = false,

                        ErrorMessage = "Token has been invalidated"

                    };

                }



                // FETCH permissions from database instead of JWT

                var permissions = await _permissionService.GetUserPermissionsAsync(validationResult.UserId.Value, cancellationToken);




                return new TokenValidationResult

                {

                    IsValid = true,

                    UserId = validationResult.UserId,

                    UserEmail = validationResult.UserEmail,

                    Roles = validationResult.Roles,

                    Permissions = permissions, // Fresh from database, not from JWT

                    Expiry = validationResult.Expiry,

                    SecurityStamp = validationResult.SecurityStamp,
                    IsExpired = isExpired,
                    IsNearExpiry = isNearExpiry

                };

            }



            return new TokenValidationResult
            {
                IsValid = true,
                UserId = validationResult.UserId,
                UserEmail = validationResult.UserEmail,
                Roles = validationResult.Roles,
                Permissions = validationResult.Permissions,
                Expiry = validationResult.Expiry,
                SecurityStamp = validationResult.SecurityStamp,
                IsExpired = isExpired,
                IsNearExpiry = isNearExpiry
            };

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error during token validation");

            return new TokenValidationResult

            {

                IsValid = false,

                ErrorMessage = "Token validation failed"

            };

        }

    }



    public async Task<CurrentUserDto?> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken = default)

    {

        try

        {

            var validation = await ValidateTokenAsync(accessToken, cancellationToken);

            if (!validation.IsValid || !validation.UserId.HasValue)

            {

                return null;

            }



            var user = await _unitOfWork.Users.GetByIdAsync(validation.UserId.Value, cancellationToken);

            if (user == null)

            {

                return null;

            }



            return new CurrentUserDto

            {

                Id = user.Id,

                Email = user.Email.Value,

                FirstName = user.FirstName,

                LastName = user.LastName,

                FullName = user.FullName,

                Role = user.Role.ToString(),

                Status = user.Status.ToString(),

                Department = user.Department,

                JobTitle = user.JobTitle,

                TimeZone = user.TimeZone,

                Language = user.Language,

                Theme = user.Theme,

                Permissions = validation.Permissions,

                LastLoginDate = user.LastLoginDate,

                PasswordChangedDate = user.PasswordChangedDate

            };

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error getting current user from token");

            return null;

        }

    }



    public async Task<bool> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)

    {

        try

        {

            var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);

            if (user == null || !user.IsValidForAuthentication())

            {

                return false;

            }



            return _passwordService.VerifyPassword(password, user.PasswordHash, user.PasswordSalt);

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error validating credentials for email: {Email}", email);

            return false;

        }

    }



    public async Task<UserLockoutStatus> GetLockoutStatusAsync(string email, CancellationToken cancellationToken = default)

    {

        var lockoutStatus = await _lockoutService.GetLockoutStatusAsync(email, cancellationToken);

        return new UserLockoutStatus

        {

            IsLockedOut = lockoutStatus.IsLockedOut,

            LockoutEnd = lockoutStatus.LockoutEnd,

            FailedAttempts = lockoutStatus.FailedAttempts,

            MaxFailedAttempts = lockoutStatus.MaxFailedAttempts,

            TimeRemaining = lockoutStatus.TimeRemaining,

            Reason = lockoutStatus.LockoutReason

        };

    }



    public async Task<bool> UnlockAccountAsync(int userId, int unlockedBy, CancellationToken cancellationToken = default)

    {

        return await _lockoutService.UnlockUserAccountAsync(userId, "Manual unlock", unlockedBy, cancellationToken);

    }



    public async Task<List<UserSessionDto>> GetUserSessionsAsync(int userId, CancellationToken cancellationToken = default)

    {

        var sessions = await _refreshTokenService.GetConcurrentSessionsAsync(userId, cancellationToken);

        return sessions.Select(s => new UserSessionDto

        {

            SessionId = s.SessionId,

            DeviceFingerprint = s.DeviceFingerprint,

            IpAddress = s.IpAddress,

            UserAgent = s.UserAgent,

            CreatedOn = s.LoginTime,

            ExpiryDate = s.ExpiryTime,

            LastUsed = s.LastActivity,

            IsActive = s.IsActive,

            DeviceType = s.DeviceType,

            Location = s.Location ?? "Unknown",

            IsCurrent = s.IsCurrent

        }).ToList();

    }



    public async Task<bool> TerminateSessionAsync(int userId, int sessionId, CancellationToken cancellationToken = default)

    {

        // Session ID corresponds to refresh token ID

        return await _refreshTokenService.RevokeRefreshTokenAsync(sessionId, "Session terminated by user",

            cancellationToken: cancellationToken);

    }



    public CookieOptions GetSecureCookieOptions(bool isSecure = true, SameSiteMode sameSite = SameSiteMode.Lax)

    {

        return new CookieOptions

        {

            HttpOnly = true,

            Secure = isSecure,

            SameSite = sameSite,

            Path = "/",

            Domain = null, // Let browser determine

            IsEssential = true

        };

    }



    public void SetAuthenticationCookies(HttpResponse response, AuthenticationResult authResult, bool isSecure = true)

    {

        if (!authResult.IsSuccess || string.IsNullOrEmpty(authResult.AccessToken) || string.IsNullOrEmpty(authResult.RefreshToken))

        {

            return;

        }



        // For HTTP localhost development, always use Lax and Secure=false

        var cookieOptions = new CookieOptions

        {

            HttpOnly = true,

            Secure = false,  // HTTP localhost requires false

            SameSite = SameSiteMode.Lax,  // Lax works for localhost

            Path = "/",

            IsEssential = true

        };



        // Set access token with expiry

        var accessTokenOptions = new CookieOptions

        {

            HttpOnly = cookieOptions.HttpOnly,

            Secure = cookieOptions.Secure,

            SameSite = cookieOptions.SameSite,

            Path = cookieOptions.Path,

            IsEssential = cookieOptions.IsEssential,

            Expires = authResult.AccessTokenExpiry

        };



        // Set refresh token with expiry

        var refreshTokenOptions = new CookieOptions

        {

            HttpOnly = cookieOptions.HttpOnly,

            Secure = cookieOptions.Secure,

            SameSite = cookieOptions.SameSite,

            Path = cookieOptions.Path,

            IsEssential = cookieOptions.IsEssential,

            Expires = authResult.RefreshTokenExpiry

        };



        response.Cookies.Append("access_token", authResult.AccessToken, accessTokenOptions);

        response.Cookies.Append("refresh_token", authResult.RefreshToken, refreshTokenOptions);



        _logger.LogInformation("🍪 HTTP Localhost cookies set - Access: {AccessLength} chars, Refresh: {RefreshLength} chars",

            authResult.AccessToken.Length, authResult.RefreshToken.Length);

    }



    // Also update ClearAuthenticationCookies method:

    public void ClearAuthenticationCookies(HttpResponse response, bool isSecure = false)

    {

        var cookieOptions = new CookieOptions

        {

            HttpOnly = true,

            Secure = false,  // HTTP localhost

            SameSite = SameSiteMode.Lax,  // Lax for localhost

            Path = "/",

            Expires = DateTime.UtcNow.AddDays(-1) // Expire immediately

        };



        response.Cookies.Append("access_token", "", cookieOptions);

        response.Cookies.Append("refresh_token", "", cookieOptions);



        _logger.LogDebug("Authentication cookies cleared");

    }



    public CookieTokenInfo? ExtractTokensFromCookies(HttpRequest request)

    {

        var accessToken = request.Cookies["access_token"];

        var refreshToken = request.Cookies["refresh_token"];



        if (string.IsNullOrEmpty(accessToken) && string.IsNullOrEmpty(refreshToken))

        {

            return null;

        }



        return new CookieTokenInfo

        {

            AccessToken = accessToken,

            RefreshToken = refreshToken

        };

    }



    private static string GetStatusErrorMessage(UserStatus status)

    {

        return status switch

        {

            UserStatus.Inactive => ValidationMessages.User.AccountInactive,

            UserStatus.Suspended => ValidationMessages.User.AccountSuspended,

            UserStatus.Locked => ValidationMessages.User.AccountLocked,

            UserStatus.Archived => ValidationMessages.User.AccountArchived,

            UserStatus.Pending => "Account activation is pending",

            UserStatus.PasswordExpired => ValidationMessages.User.PasswordExpired,

            _ => "Account is not accessible"

        };

    }

}