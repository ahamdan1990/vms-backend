using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Configuration;
using Microsoft.Extensions.Options;

namespace VisitorManagementSystem.Api.Application.Services.Email;

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
}

/// <summary>
/// Email template service implementation
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly EmailConfiguration _emailConfig;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(IOptions<EmailConfiguration> emailConfig, ILogger<EmailTemplateService> logger)
    {
        _emailConfig = emailConfig.Value;
        _logger = logger;
    }

    public async Task<string> GenerateInvitationApprovedTemplateAsync(Invitation invitation)
    {
        var template = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Invitation Approved</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ max-width: 200px; height: auto; }}
        h1 {{ color: #333; margin-bottom: 20px; }}
        .invitation-details {{ background-color: #f8f9fa; padding: 20px; border-radius: 6px; margin: 20px 0; }}
        .detail-row {{ margin: 10px 0; }}
        .label {{ font-weight: bold; color: #555; }}
        .qr-section {{ text-align: center; margin: 30px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
        .button {{ display: inline-block; padding: 12px 25px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            {GetLogoHtml()}
            <h1>Your Invitation Has Been Approved! ✅</h1>
        </div>
        
        <p>Dear {invitation.Visitor?.FullName},</p>
        
        <p>Great news! Your invitation to visit our facility has been approved. Here are the details of your upcoming visit:</p>
        
        <div class='invitation-details'>
            <div class='detail-row'><span class='label'>Invitation Number:</span> {invitation.InvitationNumber}</div>
            <div class='detail-row'><span class='label'>Host:</span> {invitation.Host?.FullName}</div>
            <div class='detail-row'><span class='label'>Purpose:</span> {invitation.VisitPurpose?.Name ?? "General Visit"}</div>
            <div class='detail-row'><span class='label'>Date & Time:</span> {invitation.ScheduledStartTime:dddd, MMMM dd, yyyy} at {invitation.ScheduledStartTime:h:mm tt}</div>
            <div class='detail-row'><span class='label'>Duration:</span> {invitation.ScheduledStartTime:h:mm tt} - {invitation.ScheduledEndTime:h:mm tt}</div>
            <div class='detail-row'><span class='label'>Location:</span> {invitation.Location?.Name ?? "Main Building"}</div>
            {(string.IsNullOrEmpty(invitation.SpecialInstructions) ? "" : $"<div class='detail-row'><span class='label'>Special Instructions:</span> {invitation.SpecialInstructions}</div>")}
        </div>

        <div class='qr-section'>
            <p><strong>QR Code for Check-in:</strong></p>
            <p>Present this QR code at reception for quick check-in.</p>
            <p><em>QR Code: {invitation.QrCode}</em></p>
        </div>

        <p><strong>What to bring:</strong></p>
        <ul>
            <li>Valid government-issued ID</li>
            <li>This email confirmation</li>
            {(invitation.RequiresBadge ? "<li>Please allow extra time for badge processing</li>" : "")}
        </ul>

        <p><strong>Important Notes:</strong></p>
        <ul>
            <li>Please arrive 10-15 minutes early for check-in</li>
            <li>Contact your host if you need to reschedule</li>
            {(invitation.RequiresEscort ? "<li>An escort will be provided during your visit</li>" : "")}
            {(!string.IsNullOrEmpty(invitation.ParkingInstructions) ? $"<li>Parking: {invitation.ParkingInstructions}</li>" : "")}
        </ul>

        <p>If you have any questions, please contact your host {invitation.Host?.FullName} or our reception.</p>
        
        <p>We look forward to your visit!</p>

        {GetFooterHtml()}
    </div>
</body>
</html>";

        return await Task.FromResult(template);
    }
    public async Task<string> GeneratePasswordResetTemplateAsync(User user, string resetToken)
    {
        var resetUrl = $"{_emailConfig.CompanyWebsiteUrl}/reset-password?token={resetToken}";
        
        var template = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Password Reset Request</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        h1 {{ color: #333; margin-bottom: 20px; }}
        .button {{ display: inline-block; padding: 12px 25px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; text-align: center; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; color: #856404; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            {GetLogoHtml()}
            <h1>Password Reset Request 🔒</h1>
        </div>
        
        <p>Dear {user.FullName},</p>
        
        <p>We received a request to reset your password for your Visitor Management System account.</p>
        
        <div style='text-align: center;'>
            <a href='{resetUrl}' class='button'>Reset Password</a>
        </div>
        
        <p>If the button doesn't work, copy and paste this link in your browser:</p>
        <p style='word-break: break-all; background-color: #f8f9fa; padding: 10px; border-radius: 4px;'>{resetUrl}</p>
        
        <div class='warning'>
            <strong>Security Notice:</strong>
            <ul>
                <li>This link will expire in 1 hour</li>
                <li>If you didn't request this reset, please ignore this email</li>
                <li>For security, this link can only be used once</li>
            </ul>
        </div>
        
        <p>If you need assistance, contact our support team at {_emailConfig.SupportEmail}.</p>
        
        {GetFooterHtml()}
    </div>
</body>
</html>";

        return await Task.FromResult(template);
    }

    public async Task<string> GeneratePdfInvitationTemplateAsync(User host)
    {
        var template = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>PDF Invitation Template</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        h1 {{ color: #333; margin-bottom: 20px; }}
        .step {{ margin: 20px 0; padding: 15px; background-color: #f8f9fa; border-left: 4px solid #007bff; }}
        .step-number {{ font-weight: bold; color: #007bff; }}
        .button {{ display: inline-block; padding: 12px 25px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            {GetLogoHtml()}
            <h1>Visitor Invitation - PDF Template</h1>
        </div>
        
        <p>Dear {host.FullName},</p>
        
        <p>You can create visitor invitations using our PDF template for visitors who cannot access the online system directly.</p>
        
        <div class='step'>
            <span class='step-number'>Step 1:</span> Download the PDF template using the link below
        </div>
        
        <div class='step'>
            <span class='step-number'>Step 2:</span> Fill in the visitor details and meeting information
        </div>
        
        <div class='step'>
            <span class='step-number'>Step 3:</span> Email the completed PDF to our admin team
        </div>
        
        <div class='step'>
            <span class='step-number'>Step 4:</span> Admin will process and create the invitation in the system
        </div>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{_emailConfig.CompanyWebsiteUrl}/api/pdf/invitation-template' class='button'>Download PDF Template</a>
        </div>
        
        <p><strong>Template Features:</strong></p>
        <ul>
            <li>Support for single or multiple visitors</li>
            <li>Visitor details (name, email, phone, company)</li>
            <li>Meeting information (date, time, purpose, location)</li>
            <li>Special requirements and instructions</li>
        </ul>
        
        <p><strong>Processing Time:</strong> PDF invitations are typically processed within 2-4 hours during business hours.</p>
        
        <p>Questions? Contact us at {_emailConfig.SupportEmail}</p>
        
        {GetFooterHtml()}
    </div>
</body>
</html>";

        return await Task.FromResult(template);
    }
    public async Task<string> GenerateVisitorCheckedInTemplateAsync(Invitation invitation)
    {
        var template = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Visitor Checked In</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        h1 {{ color: #333; }}
        .notification {{ background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 20px 0; color: #155724; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            {GetLogoHtml()}
            <h1>Visitor Arrival Notification 👋</h1>
        </div>
        
        <p>Dear {invitation.Host?.FullName},</p>
        
        <div class='notification'>
            <strong>Your visitor has arrived!</strong>
        </div>
        
        <p><strong>Visitor Details:</strong></p>
        <ul>
            <li><strong>Name:</strong> {invitation.Visitor?.FullName}</li>
            <li><strong>Company:</strong> {invitation.Visitor?.Company ?? "N/A"}</li>
            <li><strong>Check-in Time:</strong> {invitation.CheckedInAt?.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt")}</li>
            <li><strong>Meeting Purpose:</strong> {invitation.Subject}</li>
        </ul>
        
        <p>Please proceed to reception to meet your visitor.</p>
        
        {GetFooterHtml()}
    </div>
</body>
</html>";

        return await Task.FromResult(template);
    }

    public async Task<string> GenerateWelcomeTemplateAsync(User user, string temporaryPassword)
    {
        var loginUrl = $"{_emailConfig.CompanyWebsiteUrl}/login";
        
        var template = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to Visitor Management System</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        h1 {{ color: #333; }}
        .credentials {{ background-color: #f8f9fa; padding: 20px; border-radius: 6px; margin: 20px 0; }}
        .button {{ display: inline-block; padding: 12px 25px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; }}
        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; color: #856404; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            {GetLogoHtml()}
            <h1>Welcome to the Visitor Management System! 🎉</h1>
        </div>
        
        <p>Dear {user.FullName},</p>
        
        <p>Welcome! Your account has been created for the Visitor Management System. You can now manage visitor invitations and access the system.</p>
        
        <div class='credentials'>
            <p><strong>Your Login Credentials:</strong></p>
            <p><strong>Email:</strong> {user.Email}</p>
            <p><strong>Temporary Password:</strong> {temporaryPassword}</p>
        </div>
        
        <div style='text-align: center;'>
            <a href='{loginUrl}' class='button'>Login Now</a>
        </div>
        
        <div class='warning'>
            <strong>Important Security Notes:</strong>
            <ul>
                <li>Please change your password after first login</li>
                <li>Keep your credentials secure and confidential</li>
                <li>Never share your login information with others</li>
            </ul>
        </div>
        
        <p><strong>Your Role:</strong> {user.Role}</p>
        
        <p>If you have any questions or need assistance, please contact our support team at {_emailConfig.SupportEmail}.</p>
        
        {GetFooterHtml()}
    </div>
</body>
</html>";

        return await Task.FromResult(template);
    }

    private string GetLogoHtml()
    {
        if (string.IsNullOrEmpty(_emailConfig.CompanyLogoUrl))
        {
            return "<h2>Visitor Management System</h2>";
        }
        
        return $"<img src='{_emailConfig.CompanyLogoUrl}' alt='Company Logo' class='logo'>";
    }

    private string GetFooterHtml()
    {
        return $@"
        <div class='footer'>
            <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'>
            <p>This is an automated message from the Visitor Management System.</p>
            {(!string.IsNullOrEmpty(_emailConfig.CompanyWebsiteUrl) ? $"<p>Visit our website: <a href='{_emailConfig.CompanyWebsiteUrl}'>{_emailConfig.CompanyWebsiteUrl}</a></p>" : "")}
            {(!string.IsNullOrEmpty(_emailConfig.SupportEmail) ? $"<p>Support: <a href='mailto:{_emailConfig.SupportEmail}'>{_emailConfig.SupportEmail}</a></p>" : "")}
        </div>";
    }
}
