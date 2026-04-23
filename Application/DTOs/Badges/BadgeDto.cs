namespace VisitorManagementSystem.Api.Application.DTOs.Badges;

/// <summary>
/// Returned by the preview endpoint — contains the badge as base64 PDF.
/// </summary>
public class BadgePreviewDto
{
    public string PdfBase64    { get; set; } = string.Empty;
    public string ContentType  { get; set; } = "application/pdf";
    public int    FileSizeBytes { get; set; }
    public int    InvitationId  { get; set; }
    public string InvitationNumber { get; set; } = string.Empty;
}

/// <summary>
/// Returned by the print endpoint.
/// </summary>
public class BadgePrintResultDto
{
    public bool   Success       { get; set; }
    public string? ErrorMessage  { get; set; }
    public string? Protocol      { get; set; }
    /// <summary>
    /// If the backend could not print directly, the PDF is returned so the
    /// frontend can fall back to the bridge or browser window.print().
    /// </summary>
    public string? PdfBase64     { get; set; }
    public string? ZplContent    { get; set; }
    public int    InvitationId   { get; set; }
    public string InvitationNumber { get; set; } = string.Empty;
}
