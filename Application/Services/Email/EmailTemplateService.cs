using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Configuration;
using Microsoft.Extensions.Options;

namespace VisitorManagementSystem.Api.Application.Services.Email;

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

    public string GenerateQrInvitationTemplate(string visitorName, string subject, DateTime visitDate, Location location)
    {
        return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""UTF-8"">
                    <style>
                        body {{
                            font-family: Arial, sans-serif;
                            color: #333;
                            background-color: #f7f7f7;
                            padding: 20px;
                        }}
                        .container {{
                            background-color: #fff;
                            border-radius: 8px;
                            padding: 20px;
                            max-width: 600px;
                            margin: auto;
                            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                        }}
                        h2 {{
                            color: #2c3e50;
                        }}
                        .details {{
                            margin-top: 15px;
                        }}
                        .footer {{
                            font-size: 12px;
                            color: #888;
                            margin-top: 20px;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <h2>Your Visit QR Code</h2>
                        <p>Dear {visitorName},</p>
                        <p>Your visit has been scheduled. Please find your QR code attached to this email.</p>
                        <div class=""details"">
                            <p><strong>Subject:</strong> {subject}</p>
                            <p><strong>Date:</strong> {visitDate:dddd, dd MMM yyyy}</p>
                            <p><strong>Location:</strong> {location}</p>
                        </div>
                        <p>Please present this QR code when you arrive at the reception.</p>
                        <div class=""footer"">
                            <p>Thank you,<br/>Visitor Management Team</p>
                        </div>
                    </div>
                </body>
                </html>
                ";
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

    /// <summary>
    /// Generates CSV invitation template email
    /// </summary>
    public async Task<string> GenerateCsvInvitationTemplateAsync(User host, string? customMessage = null)
    {
        var htmlContent = $@"
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ text-align: center; margin-bottom: 30px; }}
                .logo {{ max-width: 200px; height: auto; }}
                .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                .steps {{ background-color: #e8f4fd; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                .footer {{ margin-top: 30px; text-align: center; color: #666; font-size: 12px; }}
                .highlight {{ background-color: #fff3cd; padding: 10px; border-radius: 5px; margin: 10px 0; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    {GetLogoHtml()}
                    <h2>Visitor Invitation Template (CSV Format)</h2>
                </div>
                
                <div class='content'>
                    <p>Hello {host.FirstName},</p>
                    
                    <p>You've requested a CSV invitation template for creating visitor invitations. This enhanced template now includes reference sheets with available hosts and visitors, making it easier to fill and more reliable to process.</p>
                    
                    {(string.IsNullOrEmpty(customMessage) ? "" : $"<p><strong>Message:</strong> {customMessage}</p>")}
                    
                    <div class='steps'>
                        <h3>How to use the enhanced CSV template:</h3>
                        <ol>
                            <li><strong>Download & Extract</strong> the attached ZIP file</li>
                            <li><strong>Review</strong> available-hosts.csv and available-visitors.csv for reference data</li>
                            <li><strong>Open</strong> invitation-template.csv in Excel, Google Sheets, or any spreadsheet application</li>
                            <li><strong>Select hosts</strong> using exact text from available-hosts.csv Display Format column</li>
                            <li><strong>Select visitors</strong> from available-visitors.csv or use 'Create New Visitor'</li>
                            <li><strong>Fill in</strong> all required fields following the instructions</li>
                            <li><strong>Save</strong> only the invitation-template.csv file in CSV format</li>
                            <li><strong>Upload</strong> the completed invitation-template.csv file through our system</li>
                        </ol>
                    </div>
                    
                    <div class='highlight'>
                        <p><strong>Enhanced Template Benefits:</strong></p>
                        <ul>
                            <li>✅ Reference sheets with active hosts and recent visitors</li>
                            <li>✅ No more guessing - exact names and details provided</li>
                            <li>✅ Prevents duplicate visitor creation</li>
                            <li>✅ Smart host selection from system users</li>
                            <li>✅ More reliable parsing and processing</li>
                            <li>✅ Support for multiple visitors in one file</li>
                            <li>✅ Clear field instructions and examples</li>
                            <li>✅ Works with any spreadsheet application</li>
                        </ul>
                    </div>
                    
                    <p><strong>Important Notes:</strong></p>
                    <ul>
                        <li>Extract the ZIP file to access all reference sheets</li>
                        <li>Use exact text from Display Format columns in reference sheets</li>
                        <li>Upload only the filled invitation-template.csv file (not the ZIP)</li>
                        <li>For hosts: copy exact text from available-hosts.csv Display Format</li>
                        <li>For visitors: copy from available-visitors.csv or use 'Create New Visitor'</li>
                        <li>Use the date format: YYYY-MM-DD HH:mm for meeting times</li>
                        <li>For boolean fields (Yes/No), use ""Yes"" or ""No""</li>
                        <li>Keep the CSV structure intact - don't add or remove columns</li>
                    </ul>
                    
                    <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
                    
                    <p>Best regards,<br>
                    Visitor Management System</p>
                </div>
                
                {GetFooterHtml()}
            </div>
        </body>
        </html>";

        return await Task.FromResult(htmlContent);
    }

    /// <summary>
    /// Generates XLSX invitation template email
    /// </summary>
    public async Task<string> GenerateXlsxInvitationTemplateAsync(User host, string? customMessage = null)
    {
        var htmlContent = $@"
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                .header {{ text-align: center; margin-bottom: 30px; }}
                .logo {{ max-width: 200px; height: auto; }}
                .content {{ background-color: #f9f9f9; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                .features {{ background-color: #e8f4fd; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                .footer {{ margin-top: 30px; text-align: center; color: #666; font-size: 12px; }}
                .highlight {{ background-color: #fff3cd; padding: 10px; border-radius: 5px; margin: 10px 0; }}
                .excel-features {{ background-color: #d4edda; padding: 15px; border-radius: 5px; margin: 15px 0; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    {GetLogoHtml()}
                    <h2>Visitor Invitation Template (Excel Format)</h2>
                </div>
                
                <div class='content'>
                    <p>Hello {host.FirstName},</p>
                    
                    <p>You've requested an Excel invitation template with intelligent dropdowns for creating visitor invitations. This advanced template provides the most user-friendly experience with built-in validation and smart features.</p>
                    
                    {(string.IsNullOrEmpty(customMessage) ? "" : $"<p><strong>Message:</strong> {customMessage}</p>")}
                    
                    <div class='excel-features'>
                        <h3>🎯 Excel Template Features:</h3>
                        <ul>
                            <li><strong>Smart Dropdowns:</strong> Host selection populated from active system users</li>
                            <li><strong>Visitor Intelligence:</strong> Choose existing visitors or create new ones</li>
                            <li><strong>Built-in Validation:</strong> Excel prevents errors before upload</li>
                            <li><strong>Professional Interface:</strong> Formatted worksheets with instructions</li>
                            <li><strong>Multiple Visitors:</strong> Easy to add multiple visitors in one file</li>
                            <li><strong>Date Pickers:</strong> Calendar integration for meeting times</li>
                        </ul>
                    </div>
                    
                    <div class='features'>
                        <h3>📋 How to use the Excel template:</h3>
                        <ol>
                            <li><strong>Download</strong> the attached Excel file (.xlsx)</li>
                            <li><strong>Open</strong> in Microsoft Excel, Google Sheets, or LibreOffice</li>
                            <li><strong>Select hosts</strong> from the dropdown (populated with active users)</li>
                            <li><strong>Choose visitors</strong> from dropdown or select ""Create New Visitor""</li>
                            <li><strong>Fill required fields</strong> (marked with * and highlighted)</li>
                            <li><strong>Use dropdowns</strong> for Yes/No fields and date pickers for times</li>
                            <li><strong>Save</strong> the file in Excel format (.xlsx)</li>
                            <li><strong>Upload</strong> through our system for processing</li>
                        </ol>
                    </div>
                    
                    <div class='highlight'>
                        <p><strong>🚀 Advantages over CSV/PDF:</strong></p>
                        <ul>
                            <li>✅ No typing errors - all selections from dropdowns</li>
                            <li>✅ Real-time validation prevents upload failures</li>
                            <li>✅ Smart host/visitor matching eliminates duplicates</li>
                            <li>✅ Professional appearance with embedded instructions</li>
                            <li>✅ Works offline once downloaded</li>
                            <li>✅ Familiar Excel interface for all users</li>
                        </ul>
                    </div>
                    
                    <p><strong>💡 Pro Tips:</strong></p>
                    <ul>
                        <li>Use the dropdowns - don't type manually to ensure accuracy</li>
                        <li>Check the Instructions worksheet for detailed guidance</li>
                        <li>Date format is automatically handled by Excel</li>
                        <li>Multiple visitor rows can be filled for group invitations</li>
                        <li>Save regularly while working on the template</li>
                    </ul>
                    
                    <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
                    
                    <p>Best regards,<br>
                    Visitor Management System</p>
                </div>
                
                {GetFooterHtml()}
            </div>
        </body>
        </html>";

        return await Task.FromResult(htmlContent);
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
