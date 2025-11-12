namespace VisitorManagementSystem.Api.Application.DTOs.Configuration;

/// <summary>
/// Represents LDAP settings exposed via the admin API.
/// </summary>
public class LdapSettingsDto
{
    public bool Enabled { get; set; }
    public string? Server { get; set; }
    public int Port { get; set; }
    public string? Domain { get; set; }
    public string? UserName { get; set; }
    public string? BaseDn { get; set; }
    public bool AutoCreateUsers { get; set; }
    public bool SyncProfileOnLogin { get; set; }
    public bool IncludeDirectoryUsersInHostSearch { get; set; }
    public string DefaultImportRole { get; set; } = string.Empty;
    public bool AllowRoleSelectionOnImport { get; set; }
    public bool HasPasswordConfigured { get; set; }
}
