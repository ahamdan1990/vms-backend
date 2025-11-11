using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;
using VisitorManagementSystem.Api.Infrastructure.Utilities;

namespace VisitorManagementSystem.Api.Application.Commands.Auth
{
    /// <summary>
    /// Handler for LDAP/Active Directory login command
    /// Authenticates users against corporate LDAP/AD and creates or updates user records
    /// </summary>
    public class LdapLoginCommandHandler : IRequestHandler<LdapLoginCommand, AuthenticationResult>
    {
        private readonly ILdapService _ldapService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<LdapLoginCommandHandler> _logger;
        private readonly LdapConfiguration _ldapConfig;
        private readonly IMapper _mapper;

        public LdapLoginCommandHandler(
            ILdapService ldapService,
            IUnitOfWork unitOfWork,
            IJwtService jwtService,
            IRefreshTokenService refreshTokenService,
            IPermissionService permissionService,
            IOptions<LdapConfiguration> ldapConfig,
            ILogger<LdapLoginCommandHandler> logger,
            IMapper mapper)
        {
            _ldapService = ldapService ?? throw new ArgumentNullException(nameof(ldapService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _ldapConfig = ldapConfig?.Value ?? throw new ArgumentNullException(nameof(ldapConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<AuthenticationResult> Handle(LdapLoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate LDAP is enabled
                if (!_ldapConfig.Enabled)
                {
                    _logger.LogWarning("LDAP login attempt but LDAP is disabled");
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "LDAP authentication is not enabled",
                        Errors = new List<string> { "LDAP service is disabled" }
                    };
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("LDAP login attempt with missing username or password");
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Username and password are required",
                        Errors = new List<string> { "Missing credentials" }
                    };
                }

                // Authenticate against LDAP/Active Directory
                _logger.LogInformation("Authenticating user via LDAP: {Username}", request.Username);
                var ldapUser = await _ldapService.AuthenticateAsync(request.Username, request.Password);

                if (ldapUser == null)
                {
                    _logger.LogWarning("LDAP authentication failed for user: {Username}", request.Username);
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid LDAP credentials",
                        Errors = new List<string> { "Authentication failed" }
                    };
                }

                // Validate LDAP user has email
                if (string.IsNullOrWhiteSpace(ldapUser.Email))
                {
                    _logger.LogWarning("LDAP user has no email address: {Username}", request.Username);
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "LDAP user email not found",
                        Errors = new List<string> { "Email address required" }
                    };
                }

                // Check if user exists in database
                var user = await _unitOfWork.Users.GetByEmailAsync(ldapUser.Email);

                if (user == null)
                {
                    // Auto-create user from LDAP if enabled
                    if (_ldapConfig.AutoCreateUsers)
                    {
                        _logger.LogInformation("Creating new user from LDAP data: {Email}", ldapUser.Email);
                        user = CreateUserFromLdap(ldapUser);
                        await _unitOfWork.Users.AddAsync(user, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning("User not found and auto-creation disabled: {Email}", ldapUser.Email);
                        return new AuthenticationResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "User account not found",
                            Errors = new List<string> { "Please contact administrator to create your account" }
                        };
                    }
                }
                else
                {
                    // Sync profile from LDAP if enabled
                    if (_ldapConfig.SyncProfileOnLogin)
                    {
                        _logger.LogInformation("Syncing user profile from LDAP: {Email}", ldapUser.Email);
                        SyncUserFromLdap(user, ldapUser);
                        _unitOfWork.Users.Update(user);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                }

                // Load user permissions
                var permissions = await _permissionService.GetUserPermissionsAsync(user.Id);

                // Generate JWT access token
                var tokenResult = await _jwtService.GenerateAccessTokenAsync(user, permissions);

                // Generate refresh token
                var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(
                    user.Id,
                    tokenResult.JwtId,
                    request.DeviceFingerprint,
                    request.IpAddress,
                    request.UserAgent);

                // Update last login timestamp
                user.LastLoginDate = DateTime.UtcNow;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Map user to CurrentUserDto
                var currentUserDto = _mapper.Map<CurrentUserDto>(user);
                currentUserDto.Permissions = permissions;

                _logger.LogInformation("LDAP login successful for user: {Email}", user.Email.Value);

                // Return authentication result
                return new AuthenticationResult
                {
                    IsSuccess = true,
                    AccessToken = tokenResult.Token,
                    AccessTokenExpiry = tokenResult.Expiry,
                    RefreshToken = refreshToken.Token,
                    RefreshTokenExpiry = refreshToken.ExpiryDate,
                    User = currentUserDto,
                    DeviceFingerprint = request.DeviceFingerprint,
                    Errors = new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing LDAP login for username: {Username}", request.Username);
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred during LDAP login",
                    Errors = new List<string> { "Internal server error occurred" }
                };
            }
        }

        /// <summary>
        /// Creates a new User entity from LDAP user data
        /// </summary>
        private User CreateUserFromLdap(LdapUserResult ldapUser)
        {
            // Validate email (should already be validated by caller, but adding for safety)
            if (string.IsNullOrWhiteSpace(ldapUser.Email))
            {
                throw new ArgumentException("LDAP user email is required", nameof(ldapUser));
            }

            var salt = CryptoHelper.GenerateSalt();

            return new User
            {
                Email = new Email(ldapUser.Email),
                FirstName = ldapUser.FirstName ?? string.Empty,
                LastName = ldapUser.LastName ?? string.Empty,
                PhoneNumber = string.IsNullOrWhiteSpace(ldapUser.Phone)
                    ? null
                    : new PhoneNumber(ldapUser.Phone),
                JobTitle = ldapUser.JobTitle,
                Department = ldapUser.Department,
                Status = UserStatus.Active,
                Role = UserRole.Staff, // LDAP users default to Staff role
                IsActive = true,
                IsEmailVerified = true, // LDAP is pre-authenticated
                IsLdapUser = true,
                LdapDistinguishedName = ldapUser.DistinguishedName,
                LastLdapSyncOn = DateTime.UtcNow,
                DepartmentId = _ldapConfig.DefaultDepartmentId,
                PasswordHash = CryptoHelper.HashPassword("ldap_disabled", salt), // LDAP users cannot use password
                PasswordSalt = salt,
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedOn = DateTime.UtcNow,
                IsDeleted = false,
                NormalizedEmail = ldapUser.Email.ToUpperInvariant(),
                TimeZone = "UTC",
                Language = "en-US",
                Theme = "light"
            };
        }

        /// <summary>
        /// Synchronizes user profile information from LDAP
        /// </summary>
        private void SyncUserFromLdap(User user, LdapUserResult ldapUser)
        {
            bool updated = false;

            if (!string.IsNullOrWhiteSpace(ldapUser.FirstName) && user.FirstName != ldapUser.FirstName)
            {
                user.FirstName = ldapUser.FirstName;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(ldapUser.LastName) && user.LastName != ldapUser.LastName)
            {
                user.LastName = ldapUser.LastName;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(ldapUser.Phone) && ldapUser.Phone.Length >= 7)
            {
                if (user.PhoneNumber?.Value != ldapUser.Phone)
                {
                    user.PhoneNumber = new PhoneNumber(ldapUser.Phone);
                    updated = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(ldapUser.JobTitle) && user.JobTitle != ldapUser.JobTitle)
            {
                user.JobTitle = ldapUser.JobTitle;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(ldapUser.Department) && user.Department != ldapUser.Department)
            {
                user.Department = ldapUser.Department;
                updated = true;
            }

            // Always update LDAP sync timestamp
            user.LastLdapSyncOn = DateTime.UtcNow;
            user.IsLdapUser = true;

            if (updated)
            {
                user.ModifiedOn = DateTime.UtcNow;
            }
        }
    }
}
