namespace VisitorManagementSystem.Api.Application.DTOs.Configuration;

/// <summary>
/// Response for LDAP connection test.
/// </summary>
public class LdapConnectionTestResponse
{
    public bool IsConnected { get; set; }
    public bool IsEnabled { get; set; }
    public string? Message { get; set; }
    public int UserCount { get; set; }
}
