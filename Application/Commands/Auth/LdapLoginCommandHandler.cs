using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Auth;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

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
        private readonly ILdapSettingsProvider _ldapSettingsProvider;
        private readonly IMapper _mapper;

        public LdapLoginCommandHandler(
            ILdapService ldapService,
            IUnitOfWork unitOfWork,
            IJwtService jwtService,
            IRefreshTokenService refreshTokenService,
            IPermissionService permissionService,
            ILdapSettingsProvider ldapSettingsProvider,
            ILogger<LdapLoginCommandHandler> logger,
            IMapper mapper)
        {
            _ldapService = ldapService ?? throw new ArgumentNullException(nameof(ldapService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _ldapSettingsProvider = ldapSettingsProvider ?? throw new ArgumentNullException(nameof(ldapSettingsProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<AuthenticationResult> Handle(LdapLoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var ldapConfig = await _ldapSettingsProvider.GetSettingsAsync(cancellationToken: cancellationToken);

                // Validate LDAP is enabled
                if (!ldapConfig.Enabled)
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
                if (ldapConfig.AutoCreateUsers)
                {
                        _logger.LogInformation("Creating new user from LDAP data: {Email}", ldapUser.Email);
                        user = LdapUserMapper.CreateUserFromLdap(
                            ldapUser,
                            ldapConfig,
                            ResolveImportRole(ldapConfig.DefaultImportRole));
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
                    if (ldapConfig.SyncProfileOnLogin)
                    {
                        _logger.LogInformation("Syncing user profile from LDAP: {Email}", ldapUser.Email);
                        LdapUserMapper.SyncUserFromLdap(user, ldapUser);
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
        
        private static UserRole ResolveImportRole(string? targetRoleName)
        {
            if (!string.IsNullOrWhiteSpace(targetRoleName) &&
                Enum.TryParse<UserRole>(targetRoleName, true, out var parsedRole))
            {
                return parsedRole;
            }

            return UserRole.Staff;
        }
    }
}
