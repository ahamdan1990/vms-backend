using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.ValueObjects;
using VisitorManagementSystem.Api.Infrastructure.Utilities;
using DomainEmail = VisitorManagementSystem.Api.Domain.ValueObjects.Email;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Helper utilities for creating and syncing <see cref="User"/> entities from LDAP directory data.
/// Centralizes the mapping logic so it can be reused by authentication handlers and provisioning workflows.
/// </summary>
public static class LdapUserMapper
{
    /// <summary>
    /// Creates a new <see cref="User"/> instance using LDAP attributes.
    /// </summary>
    /// <param name="ldapUser">LDAP lookup result.</param>
    /// <param name="ldapConfig">LDAP configuration to hydrate defaults (department, etc.).</param>
    /// <param name="targetRole">Optional role override for the imported user.</param>
    /// <returns>Populated user entity ready for persistence.</returns>
    /// <exception cref="ArgumentException">Thrown when required LDAP fields are missing.</exception>
    public static User CreateUserFromLdap(LdapUserResult ldapUser, LdapConfiguration ldapConfig, UserRole? targetRole = null)
    {
        if (ldapUser == null)
        {
            throw new ArgumentNullException(nameof(ldapUser));
        }

        if (string.IsNullOrWhiteSpace(ldapUser.Email))
        {
            throw new ArgumentException("LDAP user email is required", nameof(ldapUser));
        }

        var salt = CryptoHelper.GenerateSalt();

        var resolvedRole = targetRole ?? ResolveRole(ldapConfig.DefaultImportRole);

        return new User
        {
            Email = new DomainEmail(ldapUser.Email),
            NormalizedEmail = ldapUser.Email.ToUpperInvariant(),
            FirstName = ldapUser.FirstName ?? string.Empty,
            LastName = ldapUser.LastName ?? string.Empty,
            PhoneNumber = string.IsNullOrWhiteSpace(ldapUser.Phone)
                ? null
                : new PhoneNumber(ldapUser.Phone),
            JobTitle = ldapUser.JobTitle,
            Department = ldapUser.Department,
            DepartmentId = ldapConfig.DefaultDepartmentId,
            Status = UserStatus.Active,
            Role = resolvedRole, // Default role for imported LDAP users
            IsActive = true,
            IsEmailVerified = true,
            IsLdapUser = true,
            LdapDistinguishedName = ldapUser.DistinguishedName,
            LastLdapSyncOn = DateTime.UtcNow,
            PasswordHash = CryptoHelper.HashPassword("ldap_disabled", salt),
            PasswordSalt = salt,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedOn = DateTime.UtcNow,
            ModifiedOn = DateTime.UtcNow,
            IsDeleted = false,
            TimeZone = "UTC",
            Language = "en-US",
            Theme = "light"
        };
    }

    /// <summary>
    /// Applies LDAP data to an existing <see cref="User"/> and returns whether any fields changed.
    /// </summary>
    /// <param name="user">Existing system user.</param>
    /// <param name="ldapUser">Latest LDAP snapshot.</param>
    /// <returns>True when the entity was updated.</returns>
    public static bool SyncUserFromLdap(User user, LdapUserResult ldapUser)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (ldapUser == null) throw new ArgumentNullException(nameof(ldapUser));

        var updated = false;

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

        user.LastLdapSyncOn = DateTime.UtcNow;
        user.IsLdapUser = true;

        if (updated)
        {
            user.ModifiedOn = DateTime.UtcNow;
        }

        return updated;
    }

    private static UserRole ResolveRole(string? roleName)
    {
        if (!string.IsNullOrWhiteSpace(roleName) &&
            Enum.TryParse<UserRole>(roleName, true, out var parsedRole))
        {
            return parsedRole;
        }

        return UserRole.Staff;
    }
}
