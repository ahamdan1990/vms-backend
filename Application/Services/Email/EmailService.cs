using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using VisitorManagementSystem.Api.Configuration;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace VisitorManagementSystem.Api.Application.Services.Email;

/// <summary>
/// Email service implementation using MailKit
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IDynamicConfigurationService _dynamicConfig;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(10); // Cache for 10 minutes
    private const string EmailConfigCacheKey = "email_configuration";

    public EmailService(IDynamicConfigurationService dynamicConfig, ILogger<EmailService> logger, IMemoryCache cache)
    {
        _dynamicConfig = dynamicConfig ?? throw new ArgumentNullException(nameof(dynamicConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <summary>
    /// Gets email configuration with caching
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Populated EmailConfiguration object</returns>
    private async Task<EmailConfiguration> GetEmailConfigurationAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        if (_cache.TryGetValue(EmailConfigCacheKey, out EmailConfiguration? cachedConfig) && cachedConfig != null)
        {
            return cachedConfig;
        }

        // Load from database
        var config = await LoadEmailConfigurationFromDatabaseAsync(cancellationToken);

        // Cache the result
        _cache.Set(EmailConfigCacheKey, config, CacheExpiry);

        return config;
    }

    /// <summary>
    /// Loads all email configuration from database and populates EmailConfiguration object
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Populated EmailConfiguration object</returns>
    private async Task<EmailConfiguration> LoadEmailConfigurationFromDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all email configurations in one call
            var emailConfigs = await _dynamicConfig.GetCategoryConfigurationAsync("Email", cancellationToken);

            var config = new EmailConfiguration();

            // Map database values to EmailConfiguration properties with defaults
            config.SmtpHost = GetConfigValue<string>(emailConfigs, "SmtpHost", "localhost");
            config.SmtpPort = GetConfigValue<int>(emailConfigs, "SmtpPort", 587);
            config.EnableSsl = GetConfigValue<bool>(emailConfigs, "EnableSsl", true);
            config.Username = GetConfigValue<string>(emailConfigs, "Username", "");
            config.Password = GetConfigValue<string>(emailConfigs, "Password", "");
            config.FromEmail = GetConfigValue<string>(emailConfigs, "FromEmail", "noreply@vms.com");
            config.FromName = GetConfigValue<string>(emailConfigs, "FromName", "Visitor Management System");
            config.TimeoutSeconds = GetConfigValue<int>(emailConfigs, "TimeoutSeconds", 30);
            config.MaxAttachmentSizeMB = GetConfigValue<int>(emailConfigs, "MaxAttachmentSizeMB", 25);
            config.EnableSending = GetConfigValue<bool>(emailConfigs, "EnableSending", true);
            config.TestEmail = GetConfigValue<string>(emailConfigs, "TestEmail", null);
            config.TemplateDirectory = GetConfigValue<string>(emailConfigs, "TemplateDirectory", "EmailTemplates");
            config.CompanyLogoUrl = GetConfigValue<string>(emailConfigs, "CompanyLogoUrl", null);
            config.CompanyWebsiteUrl = GetConfigValue<string>(emailConfigs, "CompanyWebsiteUrl", null);
            config.SupportEmail = GetConfigValue<string>(emailConfigs, "SupportEmail", null);

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load email configuration from database, using defaults");

            // Return default configuration on error
            return new EmailConfiguration
            {
                SmtpHost = "localhost",
                SmtpPort = 587,
                EnableSsl = true,
                FromEmail = "noreply@vms.com",
                FromName = "Visitor Management System",
                TimeoutSeconds = 30,
                MaxAttachmentSizeMB = 25,
                EnableSending = true,
                TemplateDirectory = "EmailTemplates"
            };
        }
    }

    /// <summary>
    /// Invalidates email configuration cache (call this when email settings are updated)
    /// </summary>
    public void InvalidateConfigurationCache()
    {
        _cache.Remove(EmailConfigCacheKey);
        _logger.LogDebug("Email configuration cache invalidated");
    }

    /// <summary>
    /// Helper method to safely get configuration values with type conversion
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <param name="configs">Configuration dictionary</param>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if not found or conversion fails</param>
    /// <returns>Converted value or default</returns>
    private static T GetConfigValue<T>(Dictionary<string, object> configs, string key, T defaultValue)
    {
        try
        {
            if (configs.TryGetValue(key, out var value) && value != null)
            {
                // Handle type conversion
                if (typeof(T) == typeof(string))
                    return (T)(object)value.ToString()!;

                if (typeof(T) == typeof(int))
                    return (T)(object)Convert.ToInt32(value);

                if (typeof(T) == typeof(bool))
                    return (T)(object)Convert.ToBoolean(value);

                // For other types, try direct conversion
                return (T)Convert.ChangeType(value, typeof(T));
            }

            return defaultValue;
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage
        {
            To = to,
            Subject = subject,
            Body = body,
            IsHtml = true
        };

        await SendAsync(message, cancellationToken);
    }

    public async Task SendWithAttachmentsAsync(string to, string subject, string body,
        IEnumerable<EmailAttachment> attachments, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage
        {
            To = to,
            Subject = subject,
            Body = body,
            IsHtml = true,
            Attachments = attachments.ToList()
        };

        await SendAsync(message, cancellationToken);
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get configuration once at the beginning
            var config = await GetEmailConfigurationAsync(cancellationToken);

            if (!config.EnableSending)
            {
                await LogEmailInstead(message, config);
                return;
            }

            var mimeMessage = CreateMimeMessage(message, config);

            using var client = new SmtpClient();
            client.Timeout = (int)TimeSpan.FromSeconds(config.TimeoutSeconds).TotalMilliseconds;

            // Connect to SMTP server
            await client.ConnectAsync(config.SmtpHost, config.SmtpPort,
                config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(config.Username))
            {
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            }

            // Send the message
            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}",
                message.To, message.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}",
                message.To, message.Subject);
            throw;
        }
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await GetEmailConfigurationAsync(cancellationToken);

            return !string.IsNullOrEmpty(config.SmtpHost) &&
                   config.SmtpPort > 0 &&
                   !string.IsNullOrEmpty(config.FromEmail) &&
                   !string.IsNullOrEmpty(config.FromName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email configuration");
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await GetEmailConfigurationAsync(cancellationToken);

            using var client = new SmtpClient();
            client.Timeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;

            await client.ConnectAsync(config.SmtpHost, config.SmtpPort,
                config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrEmpty(config.Username))
            {
                await client.AuthenticateAsync(config.Username, config.Password, cancellationToken);
            }

            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email connection test failed");
            return false;
        }
    }

    private MimeMessage CreateMimeMessage(EmailMessage message, EmailConfiguration config)
    {
        var mimeMessage = new MimeMessage();

        // Set sender
        mimeMessage.From.Add(new MailboxAddress(config.FromName, config.FromEmail));

        // Set recipient
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));

        // Set CC recipients
        foreach (var cc in message.CcAddresses)
        {
            mimeMessage.Cc.Add(MailboxAddress.Parse(cc));
        }

        // Set BCC recipients
        foreach (var bcc in message.BccAddresses)
        {
            mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));
        }

        // Set subject
        mimeMessage.Subject = message.Subject;

        // Set priority
        mimeMessage.Priority = message.Priority switch
        {
            EmailPriority.Low => MessagePriority.NonUrgent,
            EmailPriority.High => MessagePriority.Urgent,
            EmailPriority.Urgent => MessagePriority.Urgent,
            _ => MessagePriority.Normal
        };

        // Create body
        var bodyBuilder = new BodyBuilder();

        if (message.IsHtml)
        {
            bodyBuilder.HtmlBody = message.Body;
        }
        else
        {
            bodyBuilder.TextBody = message.Body;
        }

        // Add attachments
        foreach (var attachment in message.Attachments)
        {
            ValidateAttachment(attachment, config);
            bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content,
                ContentType.Parse(attachment.MimeType));
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();
        return mimeMessage;
    }

    private void ValidateAttachment(EmailAttachment attachment, EmailConfiguration config)
    {
        if (attachment.Content.Length > config.MaxAttachmentSizeMB * 1024 * 1024)
        {
            throw new InvalidOperationException(
                $"Attachment '{attachment.FileName}' exceeds maximum size of {config.MaxAttachmentSizeMB}MB");
        }

        if (string.IsNullOrEmpty(attachment.FileName))
        {
            throw new ArgumentException("Attachment filename cannot be empty");
        }

        if (attachment.Content.Length == 0)
        {
            throw new ArgumentException($"Attachment '{attachment.FileName}' cannot be empty");
        }
    }

    private async Task LogEmailInstead(EmailMessage message, EmailConfiguration config)
    {
        var recipient = !string.IsNullOrEmpty(config.TestEmail) ? config.TestEmail : message.To;

        _logger.LogWarning("Email sending disabled. Would send email to {Recipient} with subject: {Subject}. " +
                          "Body length: {BodyLength} characters. Attachments: {AttachmentCount}",
                          recipient, message.Subject, message.Body.Length, message.Attachments.Count);

        // In development, you might want to save emails to a file or database
        await Task.CompletedTask;
    }

    public async Task<bool> ValidateConnectionAsync()
    {
        try
        {
            var config = await GetEmailConfigurationAsync();

            if (!config.EnableSending)
            {
                _logger.LogInformation("Email sending is disabled. Skipping connection validation.");
                return true; // Consider disabled email as "healthy"
            }

            using var client = new SmtpClient();

            // Configure the client
            client.Connect(config.SmtpHost, config.SmtpPort, config.EnableSsl);

            if (!string.IsNullOrEmpty(config.Username))
            {
                client.Authenticate(config.Username, config.Password);
            }

            // If we get here, connection is successful
            client.Disconnect(true);

            _logger.LogDebug("Email connection validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email connection validation failed");
            return false;
        }
    }
}