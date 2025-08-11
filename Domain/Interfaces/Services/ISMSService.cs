namespace VisitorManagementSystem.Api.Domain.Interfaces.Services;

/// <summary>
/// Interface for SMS service operations
/// </summary>
public interface ISMSService
{
    /// <summary>
    /// Sends an SMS message to a phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number to send to</param>
    /// <param name="message">Message content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendSMSAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an SMS with template
    /// </summary>
    /// <param name="phoneNumber">Phone number to send to</param>
    /// <param name="templateName">Template name</param>
    /// <param name="templateData">Template data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SendTemplatedSMSAsync(string phoneNumber, string templateName, object templateData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if SMS service is configured and available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if SMS service is available</returns>
    Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default);
}
