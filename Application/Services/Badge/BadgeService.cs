using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using iText.Kernel.Pdf.Xobject;

namespace VisitorManagementSystem.Api.Application.Services.Badge;

/// <summary>
/// Generates visitor badge content using iText7.
///
/// A4 badge layout (portrait, top half used — prints one badge per page):
///   ┌────────────────────────────────────────┐
///   │  [Logo]         VISITOR BADGE          │  ← header bar (dark blue)
///   ├────────────────────────────────────────┤
///   │  [Photo]   FULL NAME (large)           │
///   │            Company                     │
///   │            VIP ribbon (if applicable)  │
///   ├────────────────────────────────────────┤
///   │  Meeting: Subject                      │
///   │  Host:    Name | Department            │
///   │  Date:    Date  Time: HH:mm – HH:mm    │
///   │  Location: Room / Area                 │
///   ├────────────────────────────────────────┤
///   │  [QR code]  Invitation #  Date printed │  ← footer
///   └────────────────────────────────────────┘
/// </summary>
public sealed class BadgeService : IBadgeService
{
    // Brand colours — matches the blue/slate theme used in the VMS frontend
    private static readonly DeviceRgb ColourHeaderBg   = new(30,  64, 175);  // blue-800
    private static readonly DeviceRgb ColourHeaderText = new(255, 255, 255); // white
    private static readonly DeviceRgb ColourAccent     = new(59,  130, 246); // blue-500
    private static readonly DeviceRgb ColourVip        = new(217, 119, 6);   // amber-600
    private static readonly DeviceRgb ColourBodyBg     = new(248, 250, 252); // slate-50
    private static readonly DeviceRgb ColourBorder     = new(203, 213, 225); // slate-300
    private static readonly DeviceRgb ColourLabelText  = new(100, 116, 139); // slate-500
    private static readonly DeviceRgb ColourBodyText   = new(15,  23,  42);  // slate-900
    private static readonly DeviceRgb ColourFooterBg   = new(241, 245, 249); // slate-100

    private readonly ILogger<BadgeService> _logger;

    public BadgeService(ILogger<BadgeService> logger)
    {
        _logger = logger;
    }

    // ── PDF ────────────────────────────────────────────────────────────────────

    public async Task<byte[]> GenerateBadgePdfAsync(
        Invitation invitation,
        byte[]? visitorPhotoBytes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var ms     = new MemoryStream();
            // PDF 1.4 for maximum printer firmware compatibility (many MFP PDF kits
            // are based on Acrobat 5/6-era parsers that only handle up to 1.4).
            using var writer = new PdfWriter(ms,
                new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_4));
            using var pdf    = new PdfDocument(writer);

            // A4 portrait
            var pageSize = PageSize.A4;
            pdf.SetDefaultPageSize(pageSize);

            using var document = new Document(pdf, pageSize);
            document.SetMargins(0, 0, 0, 0); // we manage all margins manually

            var page   = pdf.AddNewPage();
            var canvas = new PdfCanvas(page);

            float pageW = pageSize.GetWidth();   // 595 pt
            float pageH = pageSize.GetHeight();  // 842 pt

            // The badge occupies the top 380 pt of the A4 page.
            // The remaining space is left blank so the user can print two badges
            // per sheet by duplicating (or just ignore the empty area for full-page print).
            float badgeH = 380f;
            float margin = 24f;

            // ── Background ────────────────────────────────────────────────────
            canvas.SetFillColor(ColourBodyBg);
            canvas.Rectangle(0, pageH - badgeH, pageW, badgeH);
            canvas.Fill();

            // Outer border
            canvas.SetStrokeColor(ColourBorder);
            canvas.SetLineWidth(1f);
            canvas.Rectangle(margin / 2, pageH - badgeH + margin / 2, pageW - margin, badgeH - margin);
            canvas.Stroke();

            // ── Header bar ────────────────────────────────────────────────────
            float headerH = 50f;
            canvas.SetFillColor(ColourHeaderBg);
            canvas.Rectangle(margin / 2, pageH - (margin / 2) - headerH, pageW - margin, headerH);
            canvas.Fill();

            // "VISITOR BADGE" text centred in header
            var boldFont  = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regFont   = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            canvas.BeginText()
                  .SetFontAndSize(boldFont, 16)
                  .SetFillColor(ColourHeaderText)
                  .MoveText(pageW / 2 - 60, pageH - margin / 2 - headerH + 16)
                  .ShowText("VISITOR BADGE")
                  .EndText();

            // Invitation number (right-aligned in header)
            canvas.BeginText()
                  .SetFontAndSize(regFont, 9)
                  .SetFillColor(new DeviceRgb(147, 197, 253)) // blue-300
                  .MoveText(pageW - margin * 4, pageH - margin / 2 - headerH + 16)
                  .ShowText($"# {invitation.InvitationNumber}")
                  .EndText();

            // ── Photo area ────────────────────────────────────────────────────
            float photoY     = pageH - margin / 2 - headerH - 130 - 10;
            float photoSize  = 100f;
            float photoX     = margin + 10;

            if (visitorPhotoBytes is { Length: > 0 })
            {
                try
                {
                    var imgData = ImageDataFactory.Create(visitorPhotoBytes);
                    var img     = new PdfImageXObject(imgData);

                    // Draw a circular clip by using a square image with rounded corners approximation
                    canvas.SaveState()
                          .Circle(photoX + photoSize / 2, photoY + photoSize / 2, photoSize / 2)
                          .Clip()
                          .EndPath();

                    canvas.AddXObjectWithTransformationMatrix(
                        img,
                        photoSize, 0,
                        0, photoSize,
                        photoX, photoY
                    );
                    canvas.RestoreState();

                    // Circular border
                    canvas.SetStrokeColor(ColourAccent)
                          .SetLineWidth(2f)
                          .Circle(photoX + photoSize / 2, photoY + photoSize / 2, photoSize / 2)
                          .Stroke();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not embed visitor photo in badge PDF.");
                    DrawPhotoPlaceholder(canvas, photoX, photoY, photoSize, boldFont);
                }
            }
            else
            {
                DrawPhotoPlaceholder(canvas, photoX, photoY, photoSize, boldFont);
            }

            // ── Visitor name & company ────────────────────────────────────────
            float nameX   = photoX + photoSize + 16;
            float nameTopY = photoY + photoSize;

            // Full name
            canvas.BeginText()
                  .SetFontAndSize(boldFont, 22)
                  .SetFillColor(ColourBodyText)
                  .MoveText(nameX, nameTopY - 24)
                  .ShowText(TruncateText(invitation.Visitor?.FullName ?? "Guest", 28))
                  .EndText();

            // Company
            string company = invitation.Visitor?.CompanyEntity?.Name
                          ?? invitation.Visitor?.Company
                          ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(company))
            {
                canvas.BeginText()
                      .SetFontAndSize(regFont, 12)
                      .SetFillColor(ColourLabelText)
                      .MoveText(nameX, nameTopY - 44)
                      .ShowText(TruncateText(company, 32))
                      .EndText();
            }

            // VIP ribbon
            if (invitation.Visitor?.IsVip == true)
            {
                float vipX = nameX;
                float vipY = nameTopY - 66;
                float vipW = 60f;
                float vipH = 18f;

                canvas.SetFillColor(ColourVip)
                      .RoundRectangle(vipX, vipY, vipW, vipH, 4)
                      .Fill();

                canvas.BeginText()
                      .SetFontAndSize(boldFont, 9)
                      .SetFillColor(ColorConstants.WHITE)
                      .MoveText(vipX + 10, vipY + 5)
                      .ShowText("★  VIP GUEST")
                      .EndText();
            }

            // ── Divider ───────────────────────────────────────────────────────
            float dividerY = photoY - 10;
            canvas.SetStrokeColor(ColourBorder)
                  .SetLineWidth(0.5f)
                  .MoveTo(margin, dividerY)
                  .LineTo(pageW - margin, dividerY)
                  .Stroke();

            // ── Meeting details ───────────────────────────────────────────────
            float detailsStartY = dividerY - 16;
            float labelCol      = margin + 8;
            float valueCol      = labelCol + 80;
            float lineH         = 20f;

            void DrawDetail(string label, string value, float y)
            {
                canvas.BeginText()
                      .SetFontAndSize(boldFont, 9)
                      .SetFillColor(ColourLabelText)
                      .MoveText(labelCol, y)
                      .ShowText(label.ToUpperInvariant())
                      .EndText();

                canvas.BeginText()
                      .SetFontAndSize(regFont, 10)
                      .SetFillColor(ColourBodyText)
                      .MoveText(valueCol, y)
                      .ShowText(TruncateText(value, 55))
                      .EndText();
            }

            DrawDetail("Meeting:",  invitation.Subject, detailsStartY);
            DrawDetail("Host:",     BuildHostLine(invitation),             detailsStartY - lineH);
            DrawDetail("Date:",     invitation.ScheduledStartTime.ToString("dddd, d MMMM yyyy"), detailsStartY - lineH * 2);
            DrawDetail("Time:",     $"{invitation.ScheduledStartTime:HH:mm} – {invitation.ScheduledEndTime:HH:mm}",  detailsStartY - lineH * 3);

            if (invitation.Location is not null)
                DrawDetail("Location:", invitation.Location.Name, detailsStartY - lineH * 4);

            if (invitation.RequiresEscort)
            {
                float escortY = detailsStartY - lineH * 5;
                canvas.SetFillColor(new DeviceRgb(239, 68, 68)) // red-500
                      .RoundRectangle(labelCol, escortY - 2, 120, 16, 3)
                      .Fill();
                canvas.BeginText()
                      .SetFontAndSize(boldFont, 8)
                      .SetFillColor(ColorConstants.WHITE)
                      .MoveText(labelCol + 8, escortY + 3)
                      .ShowText("ESCORT REQUIRED")
                      .EndText();
            }

            // ── Divider ───────────────────────────────────────────────────────
            float footerDivY = margin / 2 + 50;
            canvas.SetStrokeColor(ColourBorder)
                  .SetLineWidth(0.5f)
                  .MoveTo(margin, footerDivY)
                  .LineTo(pageW - margin, footerDivY)
                  .Stroke();

            // ── Footer ────────────────────────────────────────────────────────
            canvas.SetFillColor(ColourFooterBg)
                  .Rectangle(margin / 2, margin / 2, pageW - margin, 50)
                  .Fill();

            // Printed on
            canvas.BeginText()
                  .SetFontAndSize(regFont, 8)
                  .SetFillColor(ColourLabelText)
                  .MoveText(margin + 100, margin / 2 + 8)
                  .ShowText($"Printed: {DateTime.Now:d MMM yyyy, HH:mm}")
                  .EndText();

            // QR / invitation number watermark in footer
            canvas.BeginText()
                  .SetFontAndSize(boldFont, 9)
                  .SetFillColor(ColourBodyText)
                  .MoveText(margin + 100, margin / 2 + 24)
                  .ShowText($"Invitation #{invitation.InvitationNumber}")
                  .EndText();

            document.Close();

            return await Task.FromResult(ms.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate badge PDF for invitation {Id}", invitation.Id);
            throw;
        }
    }

    // ── ZPL ───────────────────────────────────────────────────────────────────

    public string GenerateBadgeZpl(Invitation invitation)
    {
        // 4"×3" label at 203 DPI = 812×609 dots
        // ZPL II — works on all Zebra printers and compatible (TSC, Cab, Honeywell)
        var v         = invitation.Visitor;
        string name    = TruncateText(v?.FullName ?? "Guest", 24);
        string company = TruncateText(v?.CompanyEntity?.Name ?? v?.Company ?? "", 28);
        string host    = TruncateText(invitation.Host?.FullName ?? "", 28);
        string date    = invitation.ScheduledStartTime.ToString("d MMM yyyy");
        string time    = $"{invitation.ScheduledStartTime:HH:mm}-{invitation.ScheduledEndTime:HH:mm}";
        string invNum  = invitation.InvitationNumber;
        string vipLine = v?.IsVip == true ? "^FO30,180^GB760,1,3^FS^FO30,188^A0N,20,20^FD★ VIP GUEST^FS" : "";
        string subject = TruncateText(invitation.Subject, 38);

        return $"""
            ^XA
            ^CI28
            ^MMT
            ^PW812
            ^LL0609
            ^LS0

            ^FO0,0^GB812,70,70^FS
            ^FO20,14^A0N,36,36^FR^FDVISITOR BADGE^FS
            ^FO560,22^A0N,22,22^FR^FD#{invNum}^FS

            ^FO30,85^A0N,42,42^FD{ZplEscape(name)}^FS
            ^FO30,135^A0N,24,24^FD{ZplEscape(company)}^FS
            {vipLine}

            ^FO30,220^GB760,1,2^FS

            ^FO30,235^A0N,20,20^FDMeeting:^FS  ^FO150,235^A0N,20,20^FD{ZplEscape(subject)}^FS
            ^FO30,265^A0N,20,20^FDHost:^FS     ^FO150,265^A0N,20,20^FD{ZplEscape(host)}^FS
            ^FO30,295^A0N,20,20^FDDate:^FS     ^FO150,295^A0N,20,20^FD{date}^FS
            ^FO30,325^A0N,20,20^FDTime:^FS     ^FO150,325^A0N,20,20^FD{time}^FS

            ^FO30,360^GB760,1,2^FS

            ^FO580,375^BQN,2,4^FDQA,{ZplEscape(invNum)}^FS

            ^FO30,380^A0N,18,18^FDPrinted: {DateTime.Now:d MMM yyyy HH:mm}^FS

            ^XZ
            """;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void DrawPhotoPlaceholder(PdfCanvas canvas, float x, float y, float size, PdfFont font)
    {
        canvas.SetFillColor(new DeviceRgb(203, 213, 225))
              .Circle(x + size / 2, y + size / 2, size / 2)
              .Fill();

        canvas.BeginText()
              .SetFontAndSize(font, 36)
              .SetFillColor(new DeviceRgb(148, 163, 184))
              .MoveText(x + size / 2 - 11, y + size / 2 - 12)
              .ShowText("?")
              .EndText();
    }

    private static string BuildHostLine(Invitation invitation)
    {
        var host = invitation.Host;
        if (host is null) return "—";

        var parts = new List<string> { host.FullName };
        if (!string.IsNullOrWhiteSpace(host.Department))
            parts.Add(host.Department);

        return string.Join(" · ", parts);
    }

    private static string TruncateText(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Length <= maxChars ? text : text[..(maxChars - 1)] + "…";
    }

    /// <summary>Escapes ZPL special characters (^ and ~).</summary>
    private static string ZplEscape(string text) =>
        text.Replace("^", " ").Replace("~", " ");
}
