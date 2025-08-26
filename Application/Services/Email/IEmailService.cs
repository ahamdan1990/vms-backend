namespace VisitorManagementSystem.Api.Application.Services.Email;

/// <summary>
/// Email service interface
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a simple email
    /// </summary>
    /// <param name="to">Recipient email</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email with attachments
    /// </summary>
    /// <param name="to">Recipient email</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body</param>
    /// <param name="attachments">Email attachments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendWithAttachmentsAsync(string to, string subject, string body,
        IEnumerable<EmailAttachment> attachments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a complex email message
    /// </summary>
    /// <param name="message">Email message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates email configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if configuration is valid</returns>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests email connectivity
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection successful</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert email for system notifications
    /// </summary>
    /// <param name="to">Recipient email</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendAlertEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the email connection and configuration (synchronous wrapper)
    /// </summary>
    /// <returns>True if connection is valid</returns>
    Task<bool> ValidateConnectionAsync();

    /// <summary>
    /// Invalidates email configuration cache (call this when email settings are updated)
    /// </summary>
    void InvalidateConfigurationCache();
}