using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Services.Auth
{
    /// <summary>
    /// LDAP authentication service for Active Directory integration
    /// Allows domain users to authenticate using their corporate credentials
    /// Supports cross-platform LDAP using Novell.Directory.Ldap.NETStandard
    /// </summary>
    public interface ILdapService
    {
        Task<LdapUserResult?> AuthenticateAsync(string username, string password);
        Task<LdapUserResult?> GetUserDetailsAsync(string username);
        Task<List<LdapUserResult>> SearchUsersAsync(string searchTerm);
        Task<List<LdapUserResult>> GetAllUsersAsync(int maxResults = 1000);
        Task<bool> TestConnectionAsync();
    }

    public class LdapService : ILdapService
    {
        private readonly ILdapSettingsProvider _ldapSettingsProvider;
        private readonly ILogger<LdapService> _logger;

        public LdapService(
            ILdapSettingsProvider ldapSettingsProvider,
            ILogger<LdapService> logger)
        {
            _ldapSettingsProvider = ldapSettingsProvider;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates user against LDAP/Active Directory
        /// Uses service account to find user, then validates credentials with user's credentials
        /// </summary>
        public async Task<LdapUserResult?> AuthenticateAsync(string username, string password)
        {
            try
            {
                var config = await _ldapSettingsProvider.GetSettingsAsync();
                if (!config.Enabled)
                {
                    _logger.LogWarning("LDAP authentication attempted while LDAP integration is disabled");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("LDAP authentication attempted with empty credentials");
                    return null;
                }

                var userDetails = await GetUserDetailsAsyncInternal(username, config);
                if (userDetails == null)
                {
                    _logger.LogWarning("LDAP user not found or could not retrieve details: {Username}", username);
                    return null;
                }

                using var userConnection = new LdapConnection();
                userConnection.SecureSocketLayer = config.Port == 636;

                try
                {
                    await userConnection.ConnectAsync(config.Server, config.Port);
                    _logger.LogDebug("Connected to LDAP server: {Server}:{Port}", config.Server, config.Port);

                    await userConnection.BindAsync(userDetails.DistinguishedName, password);
                    _logger.LogInformation("User authenticated successfully via LDAP: {Username}", username);

                    return userDetails;
                }
                catch (LdapException ex)
                {
                    _logger.LogWarning("LDAP authentication failed for user {Username}: {Message}", username, ex.LdapErrorMessage);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LDAP authentication for user: {Username}", username);
                return null;
            }
        }

        /// <summary>
        /// Retrieves user details from LDAP without authenticating
        /// Used for syncing user information and retrieving user DN for authentication
        /// </summary>
        public async Task<LdapUserResult?> GetUserDetailsAsync(string username)
        {
            var config = await _ldapSettingsProvider.GetSettingsAsync();
            if (!config.Enabled)
            {
                _logger.LogWarning("LDAP user lookup attempted while LDAP integration is disabled");
                return null;
            }

            return await GetUserDetailsAsyncInternal(username, config);
        }

        private async Task<LdapUserResult?> GetUserDetailsAsyncInternal(string username, LdapConfiguration config)
        {
            try
            {
                using var connection = new LdapConnection();
                connection.SecureSocketLayer = config.Port == 636;

                await connection.ConnectAsync(config.Server, config.Port);
                _logger.LogDebug("Connected to LDAP server for user lookup: {Username}", username);

                if (!string.IsNullOrWhiteSpace(config.UserName) && !string.IsNullOrWhiteSpace(config.Password))
                {
                    await connection.BindAsync(config.UserName, config.Password);
                    _logger.LogDebug("Service account authentication successful");
                }

                string searchFilter = $"(&(objectClass=user)(objectCategory=person)(|(sAMAccountName={EscapeLdapFilter(username)})(userPrincipalName={EscapeLdapFilter(username)}*)))";

                string[] attributesToReturn =
                {
                    "mail", "givenName", "sn", "displayName", "department",
                    "title", "telephoneNumber", "company", "manager",
                    "physicalDeliveryOfficeName", "sAMAccountName", "userPrincipalName"
                };

                var searchResults = await connection.SearchAsync(
                    config.BaseDn,
                    LdapConnection.ScopeSub,
                    searchFilter,
                    attributesToReturn,
                    false);

                await foreach (var entry in searchResults)
                {
                    var result = ExtractUserFromLdapEntry(entry, config);
                    if (result != null)
                    {
                        _logger.LogInformation("LDAP user details retrieved: {Email}", result.Email);
                        return result;
                    }
                }

                _logger.LogWarning("LDAP user not found: {Username}", username);
                return null;
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "LDAP error retrieving user details for: {Username} - {Message}", username, ex.LdapErrorMessage);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving LDAP user details for: {Username}", username);
                return null;
            }
        }

        /// <summary>
        /// Searches LDAP for users matching search criteria
        /// Used for auto-complete and user lookup
        /// </summary>
        public async Task<List<LdapUserResult>> SearchUsersAsync(string searchTerm)
        {
            var results = new List<LdapUserResult>();

            try
            {
                var config = await _ldapSettingsProvider.GetSettingsAsync();
                if (!config.Enabled)
                {
                    _logger.LogWarning("LDAP search attempted while LDAP integration is disabled");
                    return results;
                }

                if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                {
                    return results;
                }

                using var connection = new LdapConnection();
                connection.SecureSocketLayer = config.Port == 636;

                await connection.ConnectAsync(config.Server, config.Port);
                _logger.LogDebug("Connected to LDAP server for user search");

                if (!string.IsNullOrWhiteSpace(config.UserName) && !string.IsNullOrWhiteSpace(config.Password))
                {
                    await connection.BindAsync(config.UserName, config.Password);
                    _logger.LogDebug("Service account authentication successful");
                }

                string escapedTerm = EscapeLdapFilter(searchTerm);
                string searchFilter = $"(&(objectClass=user)(objectCategory=person)" +
                                     $"(|(displayName=*{escapedTerm}*)" +
                                     $"(sAMAccountName=*{escapedTerm}*)" +
                                     $"(mail=*{escapedTerm}*)" +
                                     $"(givenName=*{escapedTerm}*)" +
                                     $"(sn=*{escapedTerm}*)))";

                string[] attributesToReturn =
                {
                    "mail", "givenName", "sn", "displayName", "department",
                    "title", "telephoneNumber", "company"
                };

                var searchResults = await connection.SearchAsync(
                    config.BaseDn,
                    LdapConnection.ScopeSub,
                    searchFilter,
                    attributesToReturn,
                    false);

                int count = 0;
                const int maxResults = 20;

                await foreach (var entry in searchResults)
                {
                    if (count >= maxResults)
                    {
                        break;
                    }

                    try
                    {
                        var result = ExtractUserFromLdapEntry(entry, config);
                        if (result != null)
                        {
                            results.Add(result);
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing LDAP search result");
                    }
                }

                _logger.LogInformation("LDAP user search completed for term '{Term}': {Count} results", searchTerm, results.Count);
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "LDAP error searching for: {Term} - {Message}", searchTerm, ex.LdapErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching LDAP for: {Term}", searchTerm);
            }

            return results;
        }

        /// <summary>
        /// Gets all users from LDAP directory
        /// Used for displaying and importing domain users
        /// </summary>
        public async Task<List<LdapUserResult>> GetAllUsersAsync(int maxResults = 1000)
        {
            var results = new List<LdapUserResult>();

            try
            {
                var config = await _ldapSettingsProvider.GetSettingsAsync();
                if (!config.Enabled)
                {
                    _logger.LogWarning("LDAP get all users attempted while LDAP integration is disabled");
                    return results;
                }

                using var connection = new LdapConnection();
                connection.SecureSocketLayer = config.Port == 636;

                await connection.ConnectAsync(config.Server, config.Port);
                _logger.LogDebug("Connected to LDAP server for listing all users");

                if (!string.IsNullOrWhiteSpace(config.UserName) && !string.IsNullOrWhiteSpace(config.Password))
                {
                    await connection.BindAsync(config.UserName, config.Password);
                    _logger.LogDebug("Service account authentication successful");
                }

                // Filter for active user accounts
                string searchFilter = "(&(objectClass=user)(objectCategory=person)(!(userAccountControl:1.2.840.113556.1.4.803:=2)))";

                string[] attributesToReturn =
                {
                    "mail", "givenName", "sn", "displayName", "department",
                    "title", "telephoneNumber", "company", "sAMAccountName",
                    "physicalDeliveryOfficeName"
                };

                var searchResults = await connection.SearchAsync(
                    config.BaseDn,
                    LdapConnection.ScopeSub,
                    searchFilter,
                    attributesToReturn,
                    false);

                int count = 0;

                await foreach (var entry in searchResults)
                {
                    if (count >= maxResults)
                    {
                        _logger.LogInformation("LDAP search reached max results limit: {MaxResults}. Use pagination for more results.", maxResults);
                        break;
                    }

                    try
                    {
                        var result = ExtractUserFromLdapEntry(entry, config);
                        if (result != null && !string.IsNullOrEmpty(result.Email))
                        {
                            results.Add(result);
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing LDAP entry");
                    }
                }

                _logger.LogInformation("LDAP get all users completed: {Count} results", results.Count);
            }
            catch (LdapException ex) when (ex.ResultCode == 10) // LdapException.REFERRAL
            {
                // Referral exceptions are expected in AD environments with multiple partitions (e.g., ForestDnsZones)
                // This is not an error - just means we've reached a referral that we're not following
                _logger.LogDebug("LDAP search encountered referrals (e.g., ForestDnsZones). Ignoring as referral following is disabled. Retrieved {Count} users.", results.Count);
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "LDAP error getting all users - {Message}", ex.LdapErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all LDAP users");
            }

            return results;
        }

        /// <summary>
        /// Tests LDAP connection and authentication
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var config = await _ldapSettingsProvider.GetSettingsAsync();
                if (!config.Enabled)
                {
                    _logger.LogWarning("LDAP connection test attempted while LDAP integration is disabled");
                    return false;
                }

                using var connection = new LdapConnection();
                connection.SecureSocketLayer = config.Port == 636;

                await connection.ConnectAsync(config.Server, config.Port);
                _logger.LogDebug("Connected to LDAP server for connection test");

                if (!string.IsNullOrWhiteSpace(config.UserName) && !string.IsNullOrWhiteSpace(config.Password))
                {
                    await connection.BindAsync(config.UserName, config.Password);
                    _logger.LogInformation("LDAP connection test successful");
                    return true;
                }

                _logger.LogWarning("LDAP connection test failed: No credentials configured");
                return false;
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "LDAP connection test failed - {Message}", ex.LdapErrorMessage);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing LDAP connection");
                return false;
            }
        }

        /// <summary>
        /// Extracts LDAP attribute value from LdapEntry
        /// </summary>
        private string? GetLdapAttributeValue(LdapEntry entry, string attributeName)
        {
            try
            {
                // Check if attribute exists
                if (entry.Contains(attributeName))
                {
                    return entry.GetStringValueOrDefault(attributeName);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error extracting attribute '{attributeName}' from LDAP entry");
                return null;
            }
        }

        /// <summary>
        /// Extracts user information from LDAP entry
        /// </summary>
        private LdapUserResult? ExtractUserFromLdapEntry(LdapEntry entry, LdapConfiguration config)
        {
            try
            {
                var email = GetLdapAttributeValue(entry, "mail");
                var firstName = GetLdapAttributeValue(entry, "givenName");
                var lastName = GetLdapAttributeValue(entry, "sn");
                var displayName = GetLdapAttributeValue(entry, "displayName");
                var samAccountName = GetLdapAttributeValue(entry, "sAMAccountName");
                var userPrincipalName = GetLdapAttributeValue(entry, "userPrincipalName");

                // Email is required
                if (string.IsNullOrEmpty(email))
                {
                    // Try to construct email from sAMAccountName and domain
                    if (!string.IsNullOrEmpty(samAccountName) && !string.IsNullOrEmpty(config.Domain))
                    {
                        email = $"{samAccountName}@{config.Domain}";
                    }
                    // Or extract from userPrincipalName if it looks like an email
                    else if (!string.IsNullOrEmpty(userPrincipalName) && userPrincipalName.Contains("@"))
                    {
                        email = userPrincipalName;
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot extract email from LDAP entry: {entry.Dn}");
                        return null;
                    }
                }

                return new LdapUserResult
                {
                    Username = samAccountName ?? userPrincipalName,
                    Email = email,
                    FirstName = firstName ?? "",
                    LastName = lastName ?? "",
                    DisplayName = displayName ?? (samAccountName ?? ""),
                    Department = GetLdapAttributeValue(entry, "department") ?? "Not Specified",
                    JobTitle = GetLdapAttributeValue(entry, "title") ?? "",
                    Phone = GetLdapAttributeValue(entry, "telephoneNumber") ?? "",
                    Company = GetLdapAttributeValue(entry, "company") ?? "Not Specified",
                    Manager = GetLdapAttributeValue(entry, "manager") ?? "",
                    Office = GetLdapAttributeValue(entry, "physicalDeliveryOfficeName") ?? "",
                    IsActive = true,
                    DistinguishedName = entry.Dn
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user from LDAP entry");
                return null;
            }
        }

        /// <summary>
        /// Escapes special characters in LDAP filter values to prevent LDAP injection
        /// </summary>
        private static string EscapeLdapFilter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var escaped = new System.Text.StringBuilder();
            foreach (var c in input)
            {
                switch (c)
                {
                    case '*':
                        escaped.Append("\\2a");
                        break;
                    case '(':
                        escaped.Append("\\28");
                        break;
                    case ')':
                        escaped.Append("\\29");
                        break;
                    case '\\':
                        escaped.Append("\\5c");
                        break;
                    case '/':
                        escaped.Append("\\2f");
                        break;
                    case '\0':
                        escaped.Append("\\00");
                        break;
                    default:
                        escaped.Append(c);
                        break;
                }
            }
            return escaped.ToString();
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
        public bool IncludeDirectoryUsersInHostSearch { get; set; } = true;
        public string DefaultImportRole { get; set; } = UserRole.Staff.ToString();
        public bool AllowRoleSelectionOnImport { get; set; } = false;
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
