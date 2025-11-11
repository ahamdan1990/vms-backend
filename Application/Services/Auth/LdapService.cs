using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VisitorManagementSystem.Api.Application.Services.Auth
{
    /// <summary>
    /// LDAP authentication service for Active Directory integration
    /// Allows domain users to authenticate using their corporate credentials
    /// </summary>
    public interface ILdapService
    {
        Task<LdapUserResult?> AuthenticateAsync(string username, string password);
        Task<LdapUserResult?> GetUserDetailsAsync(string username);
        Task<List<LdapUserResult>> SearchUsersAsync(string searchTerm);
    }

    public class LdapService : ILdapService
    {
        private readonly LdapConfiguration _ldapConfig;
        private readonly ILogger<LdapService> _logger;

        public LdapService(IOptions<LdapConfiguration> ldapConfig, ILogger<LdapService> logger)
        {
            _ldapConfig = ldapConfig.Value;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates user against LDAP/Active Directory
        /// </summary>
        public async Task<LdapUserResult?> AuthenticateAsync(string username, string password)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Validate input
                    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    {
                        _logger.LogWarning("LDAP authentication attempted with empty credentials");
                        return null;
                    }

                    // Format username if domain not included
                    string userPrincipalName = username.Contains("@")
                        ? username
                        : $"{username}@{_ldapConfig.Domain}";

                    using (var context = new PrincipalContext(ContextType.Domain, _ldapConfig.Server, _ldapConfig.UserName, _ldapConfig.Password))
                    {
                        // Attempt to validate credentials
                        bool isValid = context.ValidateCredentials(userPrincipalName, password);

                        if (!isValid)
                        {
                            _logger.LogWarning($"LDAP authentication failed for user: {userPrincipalName}");
                            return null;
                        }

                        // Get user details from Active Directory
                        using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                        {
                            var userPrincipal = searcher.FindOne() as UserPrincipal;
                            if (userPrincipal?.SamAccountName == username || userPrincipal?.UserPrincipalName == userPrincipalName)
                            {
                                // Extract user details
                                var directoryEntry = userPrincipal.GetUnderlyingObject() as DirectoryEntry;

                                var result = new LdapUserResult
                                {
                                    Username = userPrincipal.SamAccountName,
                                    Email = userPrincipal.EmailAddress ?? ExtractEmailFromDirectoryEntry(directoryEntry),
                                    FirstName = userPrincipal.GivenName ?? "",
                                    LastName = userPrincipal.Surname ?? "",
                                    DisplayName = userPrincipal.DisplayName ?? "",
                                    Department = ExtractProperty(directoryEntry, "department") ?? "Not Specified",
                                    JobTitle = ExtractProperty(directoryEntry, "title") ?? "",
                                    Phone = ExtractProperty(directoryEntry, "telephoneNumber") ?? "",
                                    Company = ExtractProperty(directoryEntry, "company") ?? "Not Specified",
                                    Manager = ExtractProperty(directoryEntry, "manager") ?? "",
                                    Office = ExtractProperty(directoryEntry, "physicalDeliveryOfficeName") ?? "",
                                    IsActive = userPrincipal.Enabled ?? false,
                                    DistinguishedName = userPrincipal.DistinguishedName
                                };

                                _logger.LogInformation($"LDAP user authenticated successfully: {result.Username}");
                                return result;
                            }
                        }
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error during LDAP authentication for user: {username}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Retrieves user details from LDAP without authenticating
        /// Used for syncing user information after initial login
        /// </summary>
        public async Task<LdapUserResult?> GetUserDetailsAsync(string username)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, _ldapConfig.Server, _ldapConfig.UserName, _ldapConfig.Password))
                    {
                        UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                        if (userPrincipal == null)
                        {
                            _logger.LogWarning($"LDAP user not found: {username}");
                            return null;
                        }

                        var directoryEntry = userPrincipal.GetUnderlyingObject() as DirectoryEntry;

                        var result = new LdapUserResult
                        {
                            Username = userPrincipal.SamAccountName,
                            Email = userPrincipal.EmailAddress ?? ExtractEmailFromDirectoryEntry(directoryEntry),
                            FirstName = userPrincipal.GivenName ?? "",
                            LastName = userPrincipal.Surname ?? "",
                            DisplayName = userPrincipal.DisplayName ?? "",
                            Department = ExtractProperty(directoryEntry, "department") ?? "Not Specified",
                            JobTitle = ExtractProperty(directoryEntry, "title") ?? "",
                            Phone = ExtractProperty(directoryEntry, "telephoneNumber") ?? "",
                            Company = ExtractProperty(directoryEntry, "company") ?? "Not Specified",
                            Manager = ExtractProperty(directoryEntry, "manager") ?? "",
                            Office = ExtractProperty(directoryEntry, "physicalDeliveryOfficeName") ?? "",
                            IsActive = userPrincipal.Enabled ?? false,
                            DistinguishedName = userPrincipal.DistinguishedName
                        };

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error retrieving LDAP user details for: {username}");
                    return null;
                }
            });
        }

        /// <summary>
        /// Searches LDAP for users matching search criteria
        /// Used for auto-complete and user lookup
        /// </summary>
        public async Task<List<LdapUserResult>> SearchUsersAsync(string searchTerm)
        {
            return await Task.Run(() =>
            {
                var results = new List<LdapUserResult>();

                try
                {
                    if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 3)
                        return results;

                    using (var context = new PrincipalContext(ContextType.Domain, _ldapConfig.Server, _ldapConfig.UserName, _ldapConfig.Password))
                    {
                        var userPrincipal = new UserPrincipal(context)
                        {
                            DisplayName = $"*{searchTerm}*"
                        };

                        using (var searcher = new PrincipalSearcher(userPrincipal))
                        {
                            var principalResults = searcher.FindAll()
                                .OfType<UserPrincipal>()
                                .Where(u => u.Enabled == true)
                                .Take(20) // Limit results
                                .ToList();

                            foreach (var principal in principalResults)
                            {
                                var directoryEntry = principal.GetUnderlyingObject() as DirectoryEntry;

                                var result = new LdapUserResult
                                {
                                    Username = principal.SamAccountName,
                                    Email = principal.EmailAddress ?? ExtractEmailFromDirectoryEntry(directoryEntry),
                                    FirstName = principal.GivenName ?? "",
                                    LastName = principal.Surname ?? "",
                                    DisplayName = principal.DisplayName ?? "",
                                    Department = ExtractProperty(directoryEntry, "department") ?? "Not Specified",
                                    JobTitle = ExtractProperty(directoryEntry, "title") ?? "",
                                    Phone = ExtractProperty(directoryEntry, "telephoneNumber") ?? "",
                                    Company = ExtractProperty(directoryEntry, "company") ?? "Not Specified",
                                    IsActive = principal.Enabled ?? false
                                };

                                results.Add(result);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error searching LDAP for: {searchTerm}");
                }

                return results;
            });
        }

        /// <summary>
        /// Extracts property value from DirectoryEntry
        /// </summary>
        private string? ExtractProperty(DirectoryEntry? directoryEntry, string propertyName)
        {
            try
            {
                if (directoryEntry?.Properties?.Contains(propertyName) == true)
                {
                    return directoryEntry.Properties[propertyName][0]?.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error extracting property {propertyName} from DirectoryEntry");
            }

            return null;
        }

        /// <summary>
        /// Extracts email from DirectoryEntry if not available from UserPrincipal
        /// </summary>
        private string? ExtractEmailFromDirectoryEntry(DirectoryEntry? directoryEntry)
        {
            try
            {
                if (directoryEntry?.Properties?.Contains("mail") == true)
                {
                    return directoryEntry.Properties["mail"][0]?.ToString();
                }

                // Fallback: construct email from username and domain
                if (directoryEntry?.Properties?.Contains("sAMAccountName") == true)
                {
                    string? username = directoryEntry.Properties["sAMAccountName"][0]?.ToString();
                    return $"{username}@{_ldapConfig.Domain}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting email from DirectoryEntry");
            }

            return null;
        }
    }

    /// <summary>
    /// Configuration for LDAP connection
    /// </summary>
    public class LdapConfiguration
    {
        public bool Enabled { get; set; } = false;
        public string? Server { get; set; } // e.g., "192.168.0.10" or "ldap.company.com"
        public int Port { get; set; } = 389;
        public string? Domain { get; set; } // e.g., "company.com"
        public string? UserName { get; set; } // Service account username for LDAP binding
        public string? Password { get; set; } // Service account password
        public string? BaseDn { get; set; } // e.g., "dc=company,dc=com"
        public int? DefaultDepartmentId { get; set; } // Default department for LDAP users
        public bool AutoCreateUsers { get; set; } = true; // Auto-create users on first LDAP login
        public bool SyncProfileOnLogin { get; set; } = true; // Sync profile info from LDAP on each login
    }

    /// <summary>
    /// Result from LDAP authentication
    /// </summary>
    public class LdapUserResult
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? DisplayName { get; set; }
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Manager { get; set; }
        public string? Office { get; set; }
        public bool IsActive { get; set; }
        public string? DistinguishedName { get; set; }
    }
}
