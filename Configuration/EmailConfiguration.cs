using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Configuration;

/// <summary>
/// Email service configuration
/// </summary>
public class EmailConfiguration
{
    public const string SectionName = "Email";

    /// <summary>
    /// SMTP server host
    /// </summary>
    [Required]
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port
    /// </summary>
    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SMTP username
    /// </summary>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// From email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// From display name
    /// </summary>
    [Required]
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum attachment size in MB
    /// </summary>
    [Range(1, 100)]
    public int MaxAttachmentSizeMB { get; set; } = 25;

    /// <summary>
    /// Enable email sending (for testing/staging environments)
    /// </summary>
    public bool EnableSending { get; set; } = true;

    /// <summary>
    /// Fallback email for testing (when EnableSending is false)
    /// </summary>
    [EmailAddress]
    public string? TestEmail { get; set; }

    /// <summary>
    /// Email template directory path
    /// </summary>
    public string TemplateDirectory { get; set; } = "EmailTemplates";

    /// <summary>
    /// Company logo URL for email templates
    /// </summary>
    public string? CompanyLogoUrl { get; set; }

    /// <summary>
    /// Company website URL
    /// </summary>
    public string? CompanyWebsiteUrl { get; set; }

    /// <summary>
    /// Support email address
    /// </summary>
    [EmailAddress]
    public string? SupportEmail { get; set; }
}