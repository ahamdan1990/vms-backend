namespace VisitorManagementSystem.Api.Application.DTOs.Users;

/// <summary>
/// Represents a host search result record. Includes both local staff accounts and directory entries.
/// </summary>
public class HostSearchResultDto
{
    public int? Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Company { get; set; }

    /// <summary>
    /// Indicates whether the record already exists in the VMS database.
    /// </summary>
    public bool ExistsInSystem { get; set; } = true;

    /// <summary>
    /// True when the record originates from LDAP/AD.
    /// </summary>
    public bool IsLdapUser { get; set; }

    /// <summary>
    /// Source flag (local | directory) for UI hints.
    /// </summary>
    public string Source { get; set; } = "local";

    /// <summary>
    /// Identifier used to provision the host from the directory (often sAMAccountName or email).
    /// </summary>
    public string? DirectoryIdentifier { get; set; }
}
