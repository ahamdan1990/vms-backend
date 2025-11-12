using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Configuration;

/// <summary>
/// Request payload for updating LDAP settings through the admin UI.
/// </summary>
public class UpdateLdapSettingsRequest
{
    [Required]
    public bool Enabled { get; set; }

    [Required]
    [MaxLength(200)]
    public string Server { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 389;

    [MaxLength(200)]
    public string? Domain { get; set; }

    [MaxLength(200)]
    public string? UserName { get; set; }

    [MaxLength(200)]
    public string? Password { get; set; }

    [MaxLength(500)]
    public string? BaseDn { get; set; }

    public bool AutoCreateUsers { get; set; }
    public bool SyncProfileOnLogin { get; set; }
    public bool IncludeDirectoryUsersInHostSearch { get; set; }
    public bool AllowRoleSelectionOnImport { get; set; }

    [Required]
    [RegularExpression("Staff|Receptionist|Administrator", ErrorMessage = "Default role must be Staff, Receptionist or Administrator")]
    public string DefaultImportRole { get; set; } = VisitorManagementSystem.Api.Domain.Enums.UserRole.Staff.ToString();
}
