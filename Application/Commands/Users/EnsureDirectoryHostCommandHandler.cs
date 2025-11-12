using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Provisions or reuses a host user from the LDAP directory.
/// </summary>
public class EnsureDirectoryHostCommandHandler : IRequestHandler<EnsureDirectoryHostCommand, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILdapService _ldapService;
    private readonly ILogger<EnsureDirectoryHostCommandHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ILdapSettingsProvider _ldapSettingsProvider;

    public EnsureDirectoryHostCommandHandler(
        IUnitOfWork unitOfWork,
        ILdapService ldapService,
        ILdapSettingsProvider ldapSettingsProvider,
        ILogger<EnsureDirectoryHostCommandHandler> logger,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _ldapService = ldapService ?? throw new ArgumentNullException(nameof(ldapService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _ldapSettingsProvider = ldapSettingsProvider ?? throw new ArgumentNullException(nameof(ldapSettingsProvider));
    }

    public async Task<UserDto> Handle(EnsureDirectoryHostCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) && string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Identifier or email is required to provision a host");
        }

        var identifier = request.Identifier?.Trim();
        var preferredEmail = request.Email?.Trim();

        // Try to reuse an existing account by email (preferred) or identifier if it's an email
        User? existingUser = null;
        if (!string.IsNullOrWhiteSpace(preferredEmail))
        {
            existingUser = await _unitOfWork.Users.GetByEmailAsync(preferredEmail, cancellationToken);
        }

        if (existingUser == null && !string.IsNullOrWhiteSpace(identifier) && identifier.Contains('@'))
        {
            existingUser = await _unitOfWork.Users.GetByEmailAsync(identifier, cancellationToken);
        }

        if (existingUser != null)
        {
            EnsureUserIsActive(existingUser);
            _unitOfWork.Users.Update(existingUser);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return _mapper.Map<UserDto>(existingUser);
        }

        var ldapSettings = await _ldapSettingsProvider.GetSettingsAsync(cancellationToken: cancellationToken);
        if (!ldapSettings.Enabled)
        {
            throw new InvalidOperationException("LDAP integration is disabled. Cannot provision hosts from directory.");
        }

        var targetRole = ResolveTargetRole(ldapSettings, request.Role);

        var directoryLookupKey = identifier ?? preferredEmail!;
        var directoryUser = await _ldapService.GetUserDetailsAsync(directoryLookupKey);

        if (directoryUser == null)
        {
            throw new InvalidOperationException("Directory user not found. Please verify the identifier and try again.");
        }

        if (string.IsNullOrWhiteSpace(directoryUser.Email))
        {
            if (!string.IsNullOrWhiteSpace(preferredEmail))
            {
                directoryUser.Email = preferredEmail;
            }
            else
            {
                throw new InvalidOperationException("Directory user does not have an email address.");
            }
        }

        if (string.IsNullOrWhiteSpace(directoryUser.FirstName) && !string.IsNullOrWhiteSpace(request.FirstName))
        {
            directoryUser.FirstName = request.FirstName;
        }

        if (string.IsNullOrWhiteSpace(directoryUser.LastName) && !string.IsNullOrWhiteSpace(request.LastName))
        {
            directoryUser.LastName = request.LastName;
        }

        var newUser = LdapUserMapper.CreateUserFromLdap(directoryUser, ldapSettings, targetRole);

        await _unitOfWork.Users.AddAsync(newUser, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException dbEx) when (IsUniqueEmailViolation(dbEx))
        {
            _logger.LogWarning(dbEx, "Email conflict while provisioning host {Email}. Attempting to reuse existing account.", directoryUser.Email);
            var conflictUser = await _unitOfWork.Users.GetByEmailAsync(directoryUser.Email!, cancellationToken);
            if (conflictUser == null)
            {
                throw;
            }

            EnsureUserIsActive(conflictUser, targetRole);
            _unitOfWork.Users.Update(conflictUser);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return _mapper.Map<UserDto>(conflictUser);
        }

        _logger.LogInformation("Provisioned host {Email} from directory", newUser.Email.Value);
        return _mapper.Map<UserDto>(newUser);
    }

    private static void EnsureUserIsActive(User user, UserRole? overwriteRole = null)
    {
        user.IsActive = true;
        user.IsDeleted = false;
        user.Status = UserStatus.Active;
        if (overwriteRole.HasValue)
        {
            user.Role = overwriteRole.Value;
        }
    }

    private static bool IsUniqueEmailViolation(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return !string.IsNullOrWhiteSpace(message) &&
               message.Contains("IX_Users_NormalizedEmail", StringComparison.OrdinalIgnoreCase);
    }
    private static UserRole ResolveTargetRole(LdapConfiguration configuration, string? requestedRole)
    {
        if (!string.IsNullOrWhiteSpace(requestedRole))
        {
            if (!configuration.AllowRoleSelectionOnImport)
            {
                throw new InvalidOperationException("Role overrides are disabled for LDAP imports.");
            }

            if (!Enum.TryParse<UserRole>(requestedRole, true, out var overrideRole))
            {
                throw new InvalidOperationException($"Invalid role value: {requestedRole}");
            }

            return overrideRole;
        }

        if (!string.IsNullOrWhiteSpace(configuration.DefaultImportRole) &&
            Enum.TryParse<UserRole>(configuration.DefaultImportRole, true, out var defaultRole))
        {
            return defaultRole;
        }

        return UserRole.Staff;
    }
}
