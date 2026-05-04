using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Configuration;

public class ImportLdapUserRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}

public class BulkImportLdapUsersRequest
{
    [Required]
    [MinLength(1)]
    public List<string> Usernames { get; set; } = new();

    [Required]
    public string Role { get; set; } = string.Empty;
}

public class BulkImportLdapUsersWithRolesRequest
{
    [Required]
    [MinLength(1)]
    public List<LdapUserImportItem> Users { get; set; } = new();
}

public class LdapUserImportItem
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Response from importing LDAP users.
/// </summary>
public class ImportLdapUsersResponse
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<ImportLdapUserResult> Results { get; set; } = new();
}

public class ImportLdapUserResult
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
