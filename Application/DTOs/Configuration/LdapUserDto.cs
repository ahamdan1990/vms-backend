namespace VisitorManagementSystem.Api.Application.DTOs.Configuration;

/// <summary>
/// Represents a user from LDAP directory.
/// </summary>
public class LdapUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Office { get; set; } = string.Empty;
    public bool IsAlreadyImported { get; set; }
    public string? ExistingRole { get; set; }
}
