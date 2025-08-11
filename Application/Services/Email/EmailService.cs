using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using VisitorManagementSystem.Api.Configuration;
using Microsoft.Extensions.Options;

namespace VisitorManagementSystem.Api.Application.Services.Email;

/// <summary>
/// Email service implementation using MailKit
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailConfiguration> config, ILogger<EmailService> logger)
    {
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            if (!_config.EnableSending)
            {
                await LogEmailInstead(message);
                return;
            }

            var mimeMessage = CreateMimeMessage(message);
            
            using var client = new SmtpClient();
            client.Timeout = (int)TimeSpan.FromSeconds(_config.TimeoutSeconds).TotalMilliseconds;

            // Connect to SMTP server
            await client.ConnectAsync(_config.SmtpHost, _config.SmtpPort, 
                _config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, 
                cancellationToken);

            // Authenticate if credentials provided
            if (!string.IsNullOrEmpty(_config.Username))
            {
                await client.AuthenticateAsync(_config.Username, _config.Password, cancellationToken);
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

    public async Task<bool> ValidateConfigurationAsync()
    {
        return await Task.FromResult(
            !string.IsNullOrEmpty(_config.SmtpHost) &&
            _config.SmtpPort > 0 &&
            !string.IsNullOrEmpty(_config.FromEmail) &&
            !string.IsNullOrEmpty(_config.FromName)
        );
    }
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient();
            client.Timeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;

            await client.ConnectAsync(_config.SmtpHost, _config.SmtpPort,
                _config.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrEmpty(_config.Username))
            {
                await client.AuthenticateAsync(_config.Username, _config.Password, cancellationToken);
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

    private MimeMessage CreateMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        // Set sender
        mimeMessage.From.Add(new MailboxAddress(_config.FromName, _config.FromEmail));

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
            ValidateAttachment(attachment);
            bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, 
                ContentType.Parse(attachment.MimeType));
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();
        return mimeMessage;
    }
    private void ValidateAttachment(EmailAttachment attachment)
    {
        if (attachment.Content.Length > _config.MaxAttachmentSizeMB * 1024 * 1024)
        {
            throw new InvalidOperationException(
                $"Attachment '{attachment.FileName}' exceeds maximum size of {_config.MaxAttachmentSizeMB}MB");
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

    private async Task LogEmailInstead(EmailMessage message)
    {
        var recipient = !string.IsNullOrEmpty(_config.TestEmail) ? _config.TestEmail : message.To;
        
        _logger.LogWarning("Email sending disabled. Would send email to {Recipient} with subject: {Subject}. " +
                          "Body length: {BodyLength} characters. Attachments: {AttachmentCount}",
                          recipient, message.Subject, message.Body.Length, message.Attachments.Count);

        // In development, you might want to save emails to a file or database
        await Task.CompletedTask;
    }

    public async Task<bool> ValidateConnectionAsync()
    {
        if (!_config.EnableSending)
        {
            _logger.LogInformation("Email sending is disabled. Skipping connection validation.");
            return true; // Consider disabled email as "healthy"
        }

        try
        {
            using var client = new SmtpClient();
            
            // Configure the client
            client.Connect(_config.SmtpHost, _config.SmtpPort, _config.EnableSsl);
            
            if (!string.IsNullOrEmpty(_config.Username))
            {
                client.Authenticate(_config.Username, _config.Password);
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
