using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.DTOs.Configuration;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Controllers;
using MediatR;
using VisitorManagementSystem.Api.Application.Commands.Users;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Controllers.Admin;

/// <summary>
/// Controller for managing LDAP configuration through the admin UI.
/// </summary>
[ApiController]
[Route("api/admin/ldap-settings")]
[Authorize(Roles = "Administrator")]
public class LdapSettingsController : BaseController
{
    private readonly IDynamicConfigurationService _configurationService;
    private readonly ILdapSettingsProvider _ldapSettingsProvider;
    private readonly ILdapService _ldapService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ILogger<LdapSettingsController> _logger;

    public LdapSettingsController(
        IDynamicConfigurationService configurationService,
        ILdapSettingsProvider ldapSettingsProvider,
        ILdapService ldapService,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        ILogger<LdapSettingsController> logger)
    {
        _configurationService = configurationService;
        _ldapSettingsProvider = ldapSettingsProvider;
        _ldapService = ldapService;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _ldapSettingsProvider.GetSettingsAsync(cancellationToken: cancellationToken);

        var dto = new LdapSettingsDto
        {
            Enabled = settings.Enabled,
            Server = settings.Server,
            Port = settings.Port,
            Domain = settings.Domain,
            UserName = settings.UserName,
            BaseDn = settings.BaseDn,
            AutoCreateUsers = settings.AutoCreateUsers,
            SyncProfileOnLogin = settings.SyncProfileOnLogin,
            IncludeDirectoryUsersInHostSearch = settings.IncludeDirectoryUsersInHostSearch,
            DefaultImportRole = settings.DefaultImportRole,
            AllowRoleSelectionOnImport = settings.AllowRoleSelectionOnImport,
            HasPasswordConfigured = !string.IsNullOrWhiteSpace(settings.Password)
        };

        return SuccessResponse(dto);
    }

    [HttpPut]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateLdapSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError(GetModelStateErrors(), "Validation failed");
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        try
        {
            await SetConfigValueAsync("Enabled", request.Enabled.ToString().ToLowerInvariant(), currentUserId.Value, cancellationToken);
            await SetConfigValueAsync("Server", request.Server, currentUserId.Value, cancellationToken);
            await SetConfigValueAsync("Port", request.Port.ToString(), currentUserId.Value, cancellationToken);
            await SetConfigValueAsync("Domain", request.Domain ?? string.Empty, currentUserId.Value, cancellationToken);
            await SetConfigValueAsync("UserName", request.UserName ?? string.Empty, currentUserId.Value, cancellationToken);
            await SetConfigValueAsync("BaseDn", request.BaseDn ?? string.Empty, currentUserId.Value, cancellationToken);
            await SetConfigValueAsync("AutoCreateUsers", request.AutoCreateUsers.ToString().ToLowerInvariant(), currentUserId.Value, cancellationToken);
            await SetConfigValueAsync("SyncProfileOnLogin", request.SyncProfileOnLogin.ToString().ToLowerInvariant(), currentUserId.Value, cancellationToken);
            await SetConfigValueAsync("IncludeDirectoryUsersInHostSearch", request.IncludeDirectoryUsersInHostSearch.ToString().ToLowerInvariant(), currentUserId.Value, cancellationToken);
            await SetConfigValueAsync("AllowRoleSelectionOnImport", request.AllowRoleSelectionOnImport.ToString().ToLowerInvariant(), currentUserId.Value, cancellationToken);
            await SetConfigValueAsync("DefaultImportRole", request.DefaultImportRole, currentUserId.Value, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                await SetConfigValueAsync("Password", request.Password, currentUserId.Value, cancellationToken);
            }

            _ldapSettingsProvider.InvalidateCache();

            return SuccessResponse(message: "LDAP settings updated successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating LDAP settings");
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating LDAP settings");
            return ServerErrorResponse("Failed to update LDAP settings");
        }
    }

    private async Task SetConfigValueAsync(string key, string value, int userId, CancellationToken cancellationToken)
    {
        var success = await _configurationService.SetConfigurationValueAsync("LDAP", key, value, userId, "Updated via LDAP settings UI", cancellationToken);
        if (!success)
        {
            throw new InvalidOperationException($"Failed to update configuration LDAP.{key}");
        }
    }

    /// <summary>
    /// Tests LDAP connection and returns connection status.
    /// </summary>
    [HttpGet("test-connection")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _ldapSettingsProvider.GetSettingsAsync(cancellationToken: cancellationToken);

            if (!settings.Enabled)
            {
                return SuccessResponse(new LdapConnectionTestResponse
                {
                    IsEnabled = false,
                    IsConnected = false,
                    Message = "LDAP is not enabled",
                    UserCount = 0
                });
            }

            var isConnected = await _ldapService.TestConnectionAsync();
            int userCount = 0;

            if (isConnected)
            {
                // Try to get a sample count of users
                var users = await _ldapService.GetAllUsersAsync(maxResults: 10);
                userCount = users.Count;
            }

            return SuccessResponse(new LdapConnectionTestResponse
            {
                IsEnabled = true,
                IsConnected = isConnected,
                Message = isConnected ? "Connection successful" : "Connection failed. Please check your settings.",
                UserCount = userCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing LDAP connection");
            return SuccessResponse(new LdapConnectionTestResponse
            {
                IsEnabled = true,
                IsConnected = false,
                Message = $"Connection error: {ex.Message}",
                UserCount = 0
            });
        }
    }

    /// <summary>
    /// Gets all users from LDAP directory.
    /// </summary>
    [HttpGet("users")]
    [Authorize(Policy = Permissions.SystemConfig.Read)]
    public async Task<IActionResult> GetLdapUsers([FromQuery] int maxResults = 1000, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _ldapSettingsProvider.GetSettingsAsync(cancellationToken: cancellationToken);

            if (!settings.Enabled)
            {
                return BadRequestResponse("LDAP is not enabled");
            }

            var ldapUsers = await _ldapService.GetAllUsersAsync(maxResults);

            // Get all existing users to check which ones are already imported
            var existingUsers = await _unitOfWork.Users.GetAllAsync(cancellationToken: cancellationToken);
            var existingEmails = existingUsers.ToDictionary(
                u => u.Email.Value.ToLowerInvariant(),
                u => u.Role.ToString()
            );

            var userDtos = ldapUsers.Select(u => new LdapUserDto
            {
                Username = u.Username ?? string.Empty,
                Email = u.Email ?? string.Empty,
                FirstName = u.FirstName ?? string.Empty,
                LastName = u.LastName ?? string.Empty,
                DisplayName = u.DisplayName ?? string.Empty,
                Department = u.Department ?? string.Empty,
                JobTitle = u.JobTitle ?? string.Empty,
                Phone = u.Phone ?? string.Empty,
                Company = u.Company ?? string.Empty,
                Office = u.Office ?? string.Empty,
                IsAlreadyImported = existingEmails.ContainsKey(u.Email?.ToLowerInvariant() ?? string.Empty),
                ExistingRole = existingEmails.TryGetValue(u.Email?.ToLowerInvariant() ?? string.Empty, out var role) ? role : null
            }).ToList();

            return SuccessResponse(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving LDAP users");
            return ServerErrorResponse("Failed to retrieve LDAP users");
        }
    }

    /// <summary>
    /// Imports a single LDAP user into the system.
    /// </summary>
    [HttpPost("import-user")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> ImportUser([FromBody] ImportLdapUserRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError(GetModelStateErrors(), "Validation failed");
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        try
        {
            var settings = await _ldapSettingsProvider.GetSettingsAsync(cancellationToken: cancellationToken);
            if (!settings.Enabled)
            {
                return BadRequestResponse("LDAP is not enabled");
            }

            // Get user details from LDAP
            var ldapUser = await _ldapService.GetUserDetailsAsync(request.Username);
            if (ldapUser == null)
            {
                return NotFoundResponse($"User '{request.Username}' not found in LDAP directory");
            }

            // Check if user already exists
            if (await _unitOfWork.Users.EmailExistsAsync(ldapUser.Email ?? string.Empty, cancellationToken: cancellationToken))
            {
                return BadRequestResponse($"User with email '{ldapUser.Email}' already exists in the system");
            }

            // Create user using the CreateUserCommand
            var command = new CreateUserCommand
            {
                FirstName = ldapUser.FirstName ?? string.Empty,
                LastName = ldapUser.LastName ?? string.Empty,
                Email = ldapUser.Email ?? string.Empty,
                PhoneNumber = ldapUser.Phone,
                Role = request.Role,
                Department = ldapUser.Department,
                JobTitle = ldapUser.JobTitle,
                MustChangePassword = false, // LDAP users don't need to change password
                SendWelcomeEmail = false, // Don't send welcome email for LDAP users
                CreatedBy = currentUserId.Value
            };

            var userDto = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("LDAP user imported successfully: {Email}", ldapUser.Email);
            return SuccessResponse(userDto, "User imported successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error importing LDAP user: {Username}", request.Username);
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing LDAP user: {Username}", request.Username);
            return ServerErrorResponse("Failed to import user");
        }
    }

    /// <summary>
    /// Imports multiple LDAP users with a unified role.
    /// </summary>
    [HttpPost("import-users-bulk")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> ImportUsersBulk([FromBody] BulkImportLdapUsersRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError(GetModelStateErrors(), "Validation failed");
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        try
        {
            var settings = await _ldapSettingsProvider.GetSettingsAsync(cancellationToken: cancellationToken);
            if (!settings.Enabled)
            {
                return BadRequestResponse("LDAP is not enabled");
            }

            var response = new ImportLdapUsersResponse
            {
                TotalRequested = request.Usernames.Count
            };

            foreach (var username in request.Usernames)
            {
                try
                {
                    // Get user details from LDAP
                    var ldapUser = await _ldapService.GetUserDetailsAsync(username);
                    if (ldapUser == null)
                    {
                        response.Results.Add(new ImportLdapUserResult
                        {
                            Username = username,
                            Email = string.Empty,
                            Success = false,
                            ErrorMessage = "User not found in LDAP directory"
                        });
                        response.FailureCount++;
                        continue;
                    }

                    // Check if user already exists
                    if (await _unitOfWork.Users.EmailExistsAsync(ldapUser.Email ?? string.Empty, cancellationToken: cancellationToken))
                    {
                        response.Results.Add(new ImportLdapUserResult
                        {
                            Username = username,
                            Email = ldapUser.Email ?? string.Empty,
                            Success = false,
                            ErrorMessage = "User already exists in the system"
                        });
                        response.FailureCount++;
                        continue;
                    }

                    // Create user
                    var command = new CreateUserCommand
                    {
                        FirstName = ldapUser.FirstName ?? string.Empty,
                        LastName = ldapUser.LastName ?? string.Empty,
                        Email = ldapUser.Email ?? string.Empty,
                        PhoneNumber = ldapUser.Phone,
                        Role = request.Role,
                        Department = ldapUser.Department,
                        JobTitle = ldapUser.JobTitle,
                        MustChangePassword = false,
                        SendWelcomeEmail = false,
                        CreatedBy = currentUserId.Value
                    };

                    await _mediator.Send(command, cancellationToken);

                    response.Results.Add(new ImportLdapUserResult
                    {
                        Username = username,
                        Email = ldapUser.Email ?? string.Empty,
                        Success = true
                    });
                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing LDAP user: {Username}", username);
                    response.Results.Add(new ImportLdapUserResult
                    {
                        Username = username,
                        Email = string.Empty,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                    response.FailureCount++;
                }
            }

            _logger.LogInformation("Bulk LDAP import completed. Success: {Success}, Failure: {Failure}",
                response.SuccessCount, response.FailureCount);

            return SuccessResponse(response, $"Import completed. {response.SuccessCount} succeeded, {response.FailureCount} failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk LDAP import");
            return ServerErrorResponse("Failed to import users");
        }
    }

    /// <summary>
    /// Imports multiple LDAP users with individual roles.
    /// </summary>
    [HttpPost("import-users-with-roles")]
    [Authorize(Policy = Permissions.SystemConfig.Update)]
    public async Task<IActionResult> ImportUsersWithRoles([FromBody] BulkImportLdapUsersWithRolesRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationError(GetModelStateErrors(), "Validation failed");
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        try
        {
            var settings = await _ldapSettingsProvider.GetSettingsAsync(cancellationToken: cancellationToken);
            if (!settings.Enabled)
            {
                return BadRequestResponse("LDAP is not enabled");
            }

            var response = new ImportLdapUsersResponse
            {
                TotalRequested = request.Users.Count
            };

            foreach (var userItem in request.Users)
            {
                try
                {
                    // Get user details from LDAP
                    var ldapUser = await _ldapService.GetUserDetailsAsync(userItem.Username);
                    if (ldapUser == null)
                    {
                        response.Results.Add(new ImportLdapUserResult
                        {
                            Username = userItem.Username,
                            Email = string.Empty,
                            Success = false,
                            ErrorMessage = "User not found in LDAP directory"
                        });
                        response.FailureCount++;
                        continue;
                    }

                    // Check if user already exists
                    if (await _unitOfWork.Users.EmailExistsAsync(ldapUser.Email ?? string.Empty, cancellationToken: cancellationToken))
                    {
                        response.Results.Add(new ImportLdapUserResult
                        {
                            Username = userItem.Username,
                            Email = ldapUser.Email ?? string.Empty,
                            Success = false,
                            ErrorMessage = "User already exists in the system"
                        });
                        response.FailureCount++;
                        continue;
                    }

                    // Create user
                    var command = new CreateUserCommand
                    {
                        FirstName = ldapUser.FirstName ?? string.Empty,
                        LastName = ldapUser.LastName ?? string.Empty,
                        Email = ldapUser.Email ?? string.Empty,
                        PhoneNumber = ldapUser.Phone,
                        Role = userItem.Role,
                        Department = ldapUser.Department,
                        JobTitle = ldapUser.JobTitle,
                        MustChangePassword = false,
                        SendWelcomeEmail = false,
                        CreatedBy = currentUserId.Value
                    };

                    await _mediator.Send(command, cancellationToken);

                    response.Results.Add(new ImportLdapUserResult
                    {
                        Username = userItem.Username,
                        Email = ldapUser.Email ?? string.Empty,
                        Success = true
                    });
                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing LDAP user: {Username}", userItem.Username);
                    response.Results.Add(new ImportLdapUserResult
                    {
                        Username = userItem.Username,
                        Email = string.Empty,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                    response.FailureCount++;
                }
            }

            _logger.LogInformation("Bulk LDAP import with roles completed. Success: {Success}, Failure: {Failure}",
                response.SuccessCount, response.FailureCount);

            return SuccessResponse(response, $"Import completed. {response.SuccessCount} succeeded, {response.FailureCount} failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk LDAP import with roles");
            return ServerErrorResponse("Failed to import users");
        }
    }
}
