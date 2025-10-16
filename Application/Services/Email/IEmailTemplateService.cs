using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Services.Email
{
    /// <summary>
    /// Email template service interface
    /// </summary>
    public interface IEmailTemplateService
    {
        /// <summary>
        /// Generates invitation approved email template
        /// </summary>
        /// <param name="invitation">Invitation</param>
        /// <returns>Email content</returns>
        Task<string> GenerateInvitationApprovedTemplateAsync(Invitation invitation);

        /// <summary>
        /// Generates password reset email template
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="resetToken">Reset token</param>
        /// <returns>Email content</returns>
        Task<string> GeneratePasswordResetTemplateAsync(User user, string resetToken);

        /// <summary>
        /// Generates PDF invitation template email
        /// </summary>
        /// <param name="host">Host user</param>
        /// <returns>Email content</returns>
        Task<string> GeneratePdfInvitationTemplateAsync(User host);

        /// <summary>
        /// Generates visitor check-in notification template
        /// </summary>
        /// <param name="invitation">Invitation</param>
        /// <returns>Email content</returns>
        Task<string> GenerateVisitorCheckedInTemplateAsync(Invitation invitation);

        /// <summary>
        /// Generates welcome email template
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="temporaryPassword">Temporary password</param>
        /// <returns>Email content</returns>
        Task<string> GenerateWelcomeTemplateAsync(User user, string temporaryPassword);

        /// <summary>
        /// Generates CSV invitation template email
        /// </summary>
        /// <param name="host">Host user</param>
        /// <param name="customMessage">Custom message to include</param>
        /// <returns>Email content</returns>
        Task<string> GenerateCsvInvitationTemplateAsync(User host, string? customMessage = null);

        /// <summary>
        /// Generates XLSX invitation template email
        /// </summary>
        /// <param name="host">Host user</param>
        /// <param name="customMessage">Custom message to include</param>
        /// <returns>Email content</returns>
        Task<string> GenerateXlsxInvitationTemplateAsync(User host, string? customMessage = null);

        string GenerateQrInvitationTemplate(string visitorName, string subject, DateTime visitDate, Location location);
    }
}
