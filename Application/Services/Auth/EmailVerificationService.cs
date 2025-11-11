using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.Services.Email;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Utilities;

namespace VisitorManagementSystem.Api.Application.Services.Auth
{
    /// <summary>
    /// Service for email verification during signup process
    /// </summary>
    public interface IEmailVerificationService
    {
        Task<string?> GenerateVerificationTokenAsync(int userId);
        Task<bool> VerifyEmailAsync(int userId, string token);
        Task SendVerificationEmailAsync(string email, string firstName, string verificationLink);
        Task SendPasswordResetEmailAsync(string email, string firstName, string resetLink);
    }

    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EmailVerificationService> _logger;
        private readonly string _baseUrl;

        public EmailVerificationService(
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<EmailVerificationService> logger,
            IConfiguration configuration)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _baseUrl = configuration["BaseUrl"] ?? "https://localhost:3000";
        }

        /// <summary>
        /// Generates a unique email verification token and stores it in database
        /// Token expires in 24 hours
        /// </summary>
        public async Task<string?> GenerateVerificationTokenAsync(int userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for verification token generation: {userId}");
                    return null;
                }

                // Generate secure token
                string token = CryptoHelper.GenerateUrlSafeToken(32);

                // Store in database
                user.EmailVerificationToken = token;
                user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Email verification token generated for user: {user.Email}");
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating verification token for user: {userId}");
                return null;
            }
        }

        /// <summary>
        /// Verifies email using the provided token
        /// </summary>
        public async Task<bool> VerifyEmailAsync(int userId, string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning($"Empty verification token provided for user: {userId}");
                    return false;
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for email verification: {userId}");
                    return false;
                }

                // Check if token matches and hasn't expired
                if (user.EmailVerificationToken != token)
                {
                    _logger.LogWarning($"Invalid verification token for user: {user.Email}");
                    return false;
                }

                if (!user.EmailVerificationTokenExpiry.HasValue || user.EmailVerificationTokenExpiry < DateTime.UtcNow)
                {
                    _logger.LogWarning($"Verification token expired for user: {user.Email}");
                    return false;
                }

                // Mark email as verified
                user.IsEmailVerified = true;
                user.EmailVerifiedOn = DateTime.UtcNow;
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiry = null;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Email verified for user: {user.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying email for user: {userId}");
                return false;
            }
        }

        /// <summary>
        /// Sends email verification link to user
        /// </summary>
        public async Task SendVerificationEmailAsync(string email, string firstName, string verificationLink)
        {
            try
            {
                var template = GenerateVerificationEmailTemplate(firstName, verificationLink);

                await _emailService.SendAsync(
                    to: email,
                    subject: "Verify Your Email Address - VMS",
                    body: template);

                _logger.LogInformation($"Verification email sent to: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending verification email to: {email}");
                throw;
            }
        }

        /// <summary>
        /// Sends password reset link to user
        /// </summary>
        public async Task SendPasswordResetEmailAsync(string email, string firstName, string resetLink)
        {
            try
            {
                var template = GeneratePasswordResetEmailTemplate(firstName, resetLink);

                await _emailService.SendAsync(
                    to: email,
                    subject: "Reset Your Password - VMS",
                    body: template);

                _logger.LogInformation($"Password reset email sent to: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending password reset email to: {email}");
                throw;
            }
        }

        /// <summary>
        /// Generates HTML template for verification email
        /// </summary>
        private string GenerateVerificationEmailTemplate(string firstName, string verificationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to VMS</h1>
        </div>
        <div class=""content"">
            <p>Hi {firstName},</p>
            <p>Thank you for signing up for Visitor Management System. To complete your registration, please verify your email address by clicking the button below:</p>
            <a href=""{verificationLink}"" class=""button"">Verify Email Address</a>
            <p>Or copy and paste this link in your browser:</p>
            <p><code>{verificationLink}</code></p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't create this account, please ignore this email.</p>
            <div class=""footer"">
                <p>Best regards,<br/>VMS Team</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Generates HTML template for password reset email
        /// </summary>
        private string GeneratePasswordResetEmailTemplate(string firstName, string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Password Reset Request</h1>
        </div>
        <div class=""content"">
            <p>Hi {firstName},</p>
            <p>We received a request to reset your password. Click the button below to set a new password:</p>
            <a href=""{resetLink}"" class=""button"">Reset Password</a>
            <p>Or copy and paste this link in your browser:</p>
            <p><code>{resetLink}</code></p>
            <p>This link will expire in 1 hour.</p>
            <p>If you didn't request a password reset, please ignore this email and your password will remain unchanged.</p>
            <div class=""footer"">
                <p>Best regards,<br/>VMS Team</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }
}
