namespace VisitorManagementSystem.Api.Application.Services.Email;

/// <summary>
/// Email attachment model
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// Attachment file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Attachment content
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// MIME type of the attachment
    /// </summary>
    public string MimeType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Creates an email attachment
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="content">File content</param>
    /// <param name="mimeType">MIME type</param>
    /// <returns>Email attachment</returns>
    public static EmailAttachment Create(string fileName, byte[] content, string mimeType)
    {
        return new EmailAttachment
        {
            FileName = fileName,
            Content = content,
            MimeType = mimeType
        };
    }
}

/// <summary>
/// Email message model
/// </summary>
public class EmailMessage
{
    /// <summary>
    /// Recipient email address
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Email subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email body (HTML or plain text)
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Whether the body is HTML
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// CC recipients
    /// </summary>
    public List<string> CcAddresses { get; set; } = new();

    /// <summary>
    /// BCC recipients
    /// </summary>
    public List<string> BccAddresses { get; set; } = new();

    /// <summary>
    /// Email attachments
    /// </summary>
    public List<EmailAttachment> Attachments { get; set; } = new();

    /// <summary>
    /// Priority level
    /// </summary>
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
}

/// <summary>
/// Email priority levels
/// </summary>
public enum EmailPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}