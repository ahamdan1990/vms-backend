using iText.Forms;
using iText.Forms.Fields;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Options;
using VisitorManagementSystem.Api.Configuration;
using VisitorManagementSystem.Api.Domain.Entities;
using System.Globalization;

namespace VisitorManagementSystem.Api.Application.Services.Pdf;

/// <summary>
/// PDF service implementation using iText7
/// </summary>
public class PdfService : IPdfService
{
    private readonly EmailConfiguration _emailConfig;
    private readonly ILogger<PdfService> _logger;
    private readonly IWebHostEnvironment _environment;

    // PDF form field names
    private const string FIELD_HOST_NAME = "host_name";
    private const string FIELD_HOST_EMAIL = "host_email";
    private const string FIELD_HOST_PHONE = "host_phone";
    private const string FIELD_HOST_DEPT = "host_department";
    
    private const string FIELD_VISITOR1_FIRST = "visitor1_first_name";
    private const string FIELD_VISITOR1_LAST = "visitor1_last_name";
    private const string FIELD_VISITOR1_EMAIL = "visitor1_email";
    private const string FIELD_VISITOR1_PHONE = "visitor1_phone";
    private const string FIELD_VISITOR1_COMPANY = "visitor1_company";
    private const string FIELD_VISITOR1_ID = "visitor1_gov_id";
    private const string FIELD_VISITOR1_NATIONALITY = "visitor1_nationality";
    
    private const string FIELD_EMERGENCY1_FIRST = "emergency1_first_name";
    private const string FIELD_EMERGENCY1_LAST = "emergency1_last_name";
    private const string FIELD_EMERGENCY1_PHONE = "emergency1_phone";
    private const string FIELD_EMERGENCY1_REL = "emergency1_relationship";
    
    private const string FIELD_MEETING_SUBJECT = "meeting_subject";
    private const string FIELD_MEETING_START = "meeting_start_time";
    private const string FIELD_MEETING_END = "meeting_end_time";
    private const string FIELD_MEETING_PURPOSE = "meeting_purpose";
    private const string FIELD_MEETING_LOCATION = "meeting_location";
    private const string FIELD_MEETING_INSTRUCTIONS = "meeting_instructions";
    private const string FIELD_MEETING_ESCORT = "meeting_requires_escort";
    private const string FIELD_MEETING_BADGE = "meeting_requires_badge";
    private const string FIELD_MEETING_PARKING = "meeting_parking";

    public PdfService(IOptions<EmailConfiguration> emailConfig, ILogger<PdfService> logger, IWebHostEnvironment environment)
    {
        _emailConfig = emailConfig.Value;
        _logger = logger;
        _environment = environment;
    }
    public async Task<byte[]> GenerateInvitationTemplateAsync(bool includeMultipleVisitors = true, CancellationToken cancellationToken = default)
    {
        try
        {
            using var outputStream = new MemoryStream();
            
            // Create PDF writer and document
            using var writer = new PdfWriter(outputStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            // Set up fonts
            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var labelFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Add title
            document.Add(new Paragraph("VISITOR INVITATION FORM")
                .SetFont(titleFont)
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            // Add instructions
            document.Add(new Paragraph("Please fill out this form completely and email it to the admin team for processing.")
                .SetFont(normalFont)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            // Add separator line
            document.Add(new LineSeparator(new SolidLine()).SetMarginBottom(15));

            // Host Information Section
            AddHostSection(document, titleFont, labelFont);

            // Visitor Information Section
            if (includeMultipleVisitors)
            {
                AddMultipleVisitorSections(document, titleFont, labelFont);
            }
            else
            {
                AddSingleVisitorSection(document, titleFont, labelFont);
            }

            // Meeting Information Section
            AddMeetingSection(document, titleFont, labelFont);

            // Footer
            AddFooter(document, normalFont);

            // Close document
            document.Close();

            return await Task.FromResult(outputStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invitation template PDF");
            throw;
        }
    }
    private void AddHostSection(Document document, PdfFont titleFont, PdfFont labelFont)
    {
        // Host Information Section
        document.Add(new Paragraph("HOST INFORMATION")
            .SetFont(titleFont)
            .SetFontSize(14)
            .SetMarginTop(10)
            .SetMarginBottom(10));

        // Create table for host fields
        var hostTable = new Table(2);
        hostTable.SetWidth(UnitValue.CreatePercentValue(100));

        // Add host form fields
        hostTable.AddCell(CreateFormFieldCell("Host Name:", FIELD_HOST_NAME, labelFont));
        hostTable.AddCell(CreateFormFieldCell("Email:", FIELD_HOST_EMAIL, labelFont));
        hostTable.AddCell(CreateFormFieldCell("Phone Number:", FIELD_HOST_PHONE, labelFont));
        hostTable.AddCell(CreateFormFieldCell("Department:", FIELD_HOST_DEPT, labelFont));

        document.Add(hostTable);
        document.Add(new Paragraph().SetMarginBottom(15)); // Spacer
    }

    private void AddSingleVisitorSection(Document document, PdfFont titleFont, PdfFont labelFont)
    {
        // Visitor Information Section
        document.Add(new Paragraph("VISITOR INFORMATION")
            .SetFont(titleFont)
            .SetFontSize(14)
            .SetMarginBottom(10));

        // Create table for visitor fields
        var visitorTable = new Table(2);
        visitorTable.SetWidth(UnitValue.CreatePercentValue(100));

        // Add visitor form fields
        visitorTable.AddCell(CreateFormFieldCell("First Name:", FIELD_VISITOR1_FIRST, labelFont));
        visitorTable.AddCell(CreateFormFieldCell("Last Name:", FIELD_VISITOR1_LAST, labelFont));
        visitorTable.AddCell(CreateFormFieldCell("Email:", FIELD_VISITOR1_EMAIL, labelFont));
        visitorTable.AddCell(CreateFormFieldCell("Phone Number:", FIELD_VISITOR1_PHONE, labelFont));
        visitorTable.AddCell(CreateFormFieldCell("Company:", FIELD_VISITOR1_COMPANY, labelFont));
        visitorTable.AddCell(CreateFormFieldCell("Government ID:", FIELD_VISITOR1_ID, labelFont));
        visitorTable.AddCell(CreateFormFieldCell("Nationality:", FIELD_VISITOR1_NATIONALITY, labelFont));

        document.Add(visitorTable);

        // Emergency Contact
        AddEmergencyContactSection(document, labelFont, 1);
        document.Add(new Paragraph().SetMarginBottom(15)); // Spacer
    }

    private void AddMultipleVisitorSections(Document document, PdfFont titleFont, PdfFont labelFont)
    {
        // Multiple visitors - add up to 3 visitor sections
        for (int i = 1; i <= 3; i++)
        {
            document.Add(new Paragraph($"VISITOR {i} INFORMATION")
                .SetFont(titleFont)
                .SetFontSize(14)
                .SetMarginBottom(10));

            var visitorTable = new Table(2);
            visitorTable.SetWidth(UnitValue.CreatePercentValue(100));

            // Add visitor form fields with numbered field names
            visitorTable.AddCell(CreateFormFieldCell("First Name:", $"visitor{i}_first_name", labelFont));
            visitorTable.AddCell(CreateFormFieldCell("Last Name:", $"visitor{i}_last_name", labelFont));
            visitorTable.AddCell(CreateFormFieldCell("Email:", $"visitor{i}_email", labelFont));
            visitorTable.AddCell(CreateFormFieldCell("Phone Number:", $"visitor{i}_phone", labelFont));
            visitorTable.AddCell(CreateFormFieldCell("Company:", $"visitor{i}_company", labelFont));
            visitorTable.AddCell(CreateFormFieldCell("Government ID:", $"visitor{i}_gov_id", labelFont));
            visitorTable.AddCell(CreateFormFieldCell("Nationality:", $"visitor{i}_nationality", labelFont));

            document.Add(visitorTable);

            // Emergency Contact
            AddEmergencyContactSection(document, labelFont, i);
            document.Add(new Paragraph().SetMarginBottom(15)); // Spacer
        }
    }
    private void AddEmergencyContactSection(Document document, PdfFont labelFont, int visitorNumber)
    {
        document.Add(new Paragraph($"Emergency Contact for Visitor {visitorNumber}")
            .SetFontSize(12)
            .SetBold()
            .SetMarginTop(10)
            .SetMarginBottom(5));

        var emergencyTable = new Table(2);
        emergencyTable.SetWidth(UnitValue.CreatePercentValue(100));

        emergencyTable.AddCell(CreateFormFieldCell("First Name:", $"emergency{visitorNumber}_first_name", labelFont));
        emergencyTable.AddCell(CreateFormFieldCell("Last Name:", $"emergency{visitorNumber}_last_name", labelFont));
        emergencyTable.AddCell(CreateFormFieldCell("Phone Number:", $"emergency{visitorNumber}_phone", labelFont));
        emergencyTable.AddCell(CreateFormFieldCell("Relationship:", $"emergency{visitorNumber}_relationship", labelFont));

        document.Add(emergencyTable);
    }

    private void AddMeetingSection(Document document, PdfFont titleFont, PdfFont labelFont)
    {
        document.Add(new Paragraph("MEETING INFORMATION")
            .SetFont(titleFont)
            .SetFontSize(14)
            .SetMarginTop(10)
            .SetMarginBottom(10));

        var meetingTable = new Table(2);
        meetingTable.SetWidth(UnitValue.CreatePercentValue(100));

        meetingTable.AddCell(CreateFormFieldCell("Subject:", FIELD_MEETING_SUBJECT, labelFont));
        meetingTable.AddCell(CreateFormFieldCell("Purpose:", FIELD_MEETING_PURPOSE, labelFont));
        meetingTable.AddCell(CreateFormFieldCell("Start Date/Time:", FIELD_MEETING_START, labelFont));
        meetingTable.AddCell(CreateFormFieldCell("End Date/Time:", FIELD_MEETING_END, labelFont));
        meetingTable.AddCell(CreateFormFieldCell("Location:", FIELD_MEETING_LOCATION, labelFont));
        meetingTable.AddCell(CreateFormFieldCell("Special Instructions:", FIELD_MEETING_INSTRUCTIONS, labelFont));

        // Checkboxes for requirements
        meetingTable.AddCell(CreateCheckboxCell("Requires Escort:", FIELD_MEETING_ESCORT, labelFont));
        meetingTable.AddCell(CreateCheckboxCell("Requires Badge:", FIELD_MEETING_BADGE, labelFont));
        meetingTable.AddCell(CreateFormFieldCell("Parking Instructions:", FIELD_MEETING_PARKING, labelFont));

        document.Add(meetingTable);
    }

    private void AddFooter(Document document, PdfFont normalFont)
    {
        document.Add(new LineSeparator(new SolidLine()).SetMarginTop(20).SetMarginBottom(10));
        
        document.Add(new Paragraph("Instructions:")
            .SetFont(normalFont)
            .SetFontSize(10)
            .SetBold());

        document.Add(new List()
            .Add("Complete all required fields")
            .Add("Email this completed form to the admin team")
            .Add("Allow 2-4 hours for processing during business hours")
            .Add("You will receive confirmation once the invitation is approved")
            .SetFont(normalFont)
            .SetFontSize(9));

        if (!string.IsNullOrEmpty(_emailConfig.SupportEmail))
        {
            document.Add(new Paragraph($"Support: {_emailConfig.SupportEmail}")
                .SetFont(normalFont)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(10));
        }
    }
    private Cell CreateFormFieldCell(string label, string fieldName, PdfFont labelFont)
    {
        var cell = new Cell();
        
        // Add label
        cell.Add(new Paragraph(label)
            .SetFont(labelFont)
            .SetFontSize(10)
            .SetMarginBottom(2));

        // Add text field placeholder
        cell.Add(new Paragraph("_".PadRight(40, '_'))
            .SetFontSize(10)
            .SetMarginTop(2));

        cell.SetPadding(5);
        return cell;
    }

    private Cell CreateCheckboxCell(string label, string fieldName, PdfFont labelFont)
    {
        var cell = new Cell();
        
        // Add label with checkbox placeholder
        cell.Add(new Paragraph($"☐ {label}")
            .SetFont(labelFont)
            .SetFontSize(10));

        cell.SetPadding(5);
        return cell;
    }

    public async Task<ParsedInvitationData> ParseFilledInvitationAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        try
        {
            var parsedData = new ParsedInvitationData();

            using var pdfReader = new PdfReader(pdfStream);
            using var pdfDoc = new PdfDocument(pdfReader);
            
            var form = PdfAcroForm.GetAcroForm(pdfDoc, false);
            
            if (form == null)
            {
                // No form fields - try to parse text content
                return await ParseTextBasedPdfAsync(pdfDoc, cancellationToken);
            }

            // Parse form fields
            ParseHostInformation(form, parsedData);
            ParseVisitorInformation(form, parsedData);
            ParseMeetingInformation(form, parsedData);

            // Validate parsed data
            ValidateParsedData(parsedData);

            return await Task.FromResult(parsedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse filled invitation PDF");
            
            return new ParsedInvitationData
            {
                ValidationErrors = new List<string> { $"Failed to parse PDF: {ex.Message}" }
            };
        }
    }
    private void ParseHostInformation(PdfAcroForm form, ParsedInvitationData parsedData)
    {
        parsedData.Host.FullName = GetFieldValue(form, FIELD_HOST_NAME);
        parsedData.Host.Email = GetFieldValue(form, FIELD_HOST_EMAIL);
        parsedData.Host.PhoneNumber = GetFieldValue(form, FIELD_HOST_PHONE);
        parsedData.Host.Department = GetFieldValue(form, FIELD_HOST_DEPT);
    }

    private void ParseVisitorInformation(PdfAcroForm form, ParsedInvitationData parsedData)
    {
        // Parse up to 3 visitors
        for (int i = 1; i <= 3; i++)
        {
            var visitor = new ParsedVisitorData
            {
                FirstName = GetFieldValue(form, $"visitor{i}_first_name"),
                LastName = GetFieldValue(form, $"visitor{i}_last_name"),
                Email = GetFieldValue(form, $"visitor{i}_email"),
                PhoneNumber = GetFieldValue(form, $"visitor{i}_phone"),
                Company = GetFieldValue(form, $"visitor{i}_company"),
                GovernmentId = GetFieldValue(form, $"visitor{i}_gov_id"),
                Nationality = GetFieldValue(form, $"visitor{i}_nationality")
            };

            // Parse emergency contact
            var emergencyFirstName = GetFieldValue(form, $"emergency{i}_first_name");
            var emergencyLastName = GetFieldValue(form, $"emergency{i}_last_name");
            var emergencyPhone = GetFieldValue(form, $"emergency{i}_phone");
            var emergencyRelationship = GetFieldValue(form, $"emergency{i}_relationship");

            if (!string.IsNullOrEmpty(emergencyFirstName) || !string.IsNullOrEmpty(emergencyPhone))
            {
                visitor.EmergencyContact = new ParsedEmergencyContact
                {
                    FirstName = emergencyFirstName,
                    LastName = emergencyLastName,
                    PhoneNumber = emergencyPhone,
                    Relationship = emergencyRelationship
                };
            }

            // Only add visitor if they have required information
            if (!string.IsNullOrEmpty(visitor.FirstName) && !string.IsNullOrEmpty(visitor.LastName))
            {
                parsedData.Visitors.Add(visitor);
            }
        }
    }

    private void ParseMeetingInformation(PdfAcroForm form, ParsedInvitationData parsedData)
    {
        parsedData.Meeting.Subject = GetFieldValue(form, FIELD_MEETING_SUBJECT);
        parsedData.Meeting.Purpose = GetFieldValue(form, FIELD_MEETING_PURPOSE);
        parsedData.Meeting.Location = GetFieldValue(form, FIELD_MEETING_LOCATION);
        parsedData.Meeting.SpecialInstructions = GetFieldValue(form, FIELD_MEETING_INSTRUCTIONS);
        parsedData.Meeting.ParkingInstructions = GetFieldValue(form, FIELD_MEETING_PARKING);

        // Parse dates
        var startTimeStr = GetFieldValue(form, FIELD_MEETING_START);
        var endTimeStr = GetFieldValue(form, FIELD_MEETING_END);

        if (DateTime.TryParse(startTimeStr, out var startTime))
        {
            parsedData.Meeting.ScheduledStartTime = startTime;
        }

        if (DateTime.TryParse(endTimeStr, out var endTime))
        {
            parsedData.Meeting.ScheduledEndTime = endTime;
        }

        // Parse checkboxes
        parsedData.Meeting.RequiresEscort = GetCheckboxValue(form, FIELD_MEETING_ESCORT);
        parsedData.Meeting.RequiresBadge = GetCheckboxValue(form, FIELD_MEETING_BADGE);
    }

    private string GetFieldValue(PdfAcroForm form, string fieldName)
    {
        var field = form.GetField(fieldName);
        return field?.GetValueAsString()?.Trim() ?? string.Empty;
    }

    private bool GetCheckboxValue(PdfAcroForm form, string fieldName)
    {
        var field = form.GetField(fieldName);
        if (field is PdfButtonFormField buttonField)
        {
            return buttonField.GetValue()?.ToString() == "Yes";
        }
        return false;
    }
    private async Task<ParsedInvitationData> ParseTextBasedPdfAsync(PdfDocument pdfDoc, CancellationToken cancellationToken)
    {
        // For PDFs without form fields, implement text extraction and parsing
        // This is a simplified implementation - in production, you might use more sophisticated NLP
        
        var parsedData = new ParsedInvitationData();
        parsedData.ValidationErrors.Add("PDF does not contain form fields. Manual review required.");
        
        return await Task.FromResult(parsedData);
    }

    private void ValidateParsedData(ParsedInvitationData parsedData)
    {
        // Validate host information
        if (string.IsNullOrEmpty(parsedData.Host.FullName))
            parsedData.ValidationErrors.Add("Host name is required");
        
        if (string.IsNullOrEmpty(parsedData.Host.Email))
            parsedData.ValidationErrors.Add("Host email is required");

        // Validate at least one visitor
        if (!parsedData.Visitors.Any())
            parsedData.ValidationErrors.Add("At least one visitor is required");

        // Validate each visitor
        foreach (var visitor in parsedData.Visitors)
        {
            if (string.IsNullOrEmpty(visitor.FirstName))
                parsedData.ValidationErrors.Add($"Visitor first name is required");
            
            if (string.IsNullOrEmpty(visitor.LastName))
                parsedData.ValidationErrors.Add($"Visitor last name is required");
            
            if (string.IsNullOrEmpty(visitor.Email))
                parsedData.ValidationErrors.Add($"Visitor email is required for {visitor.FirstName} {visitor.LastName}");
        }

        // Validate meeting information
        if (string.IsNullOrEmpty(parsedData.Meeting.Subject))
            parsedData.ValidationErrors.Add("Meeting subject is required");

        if (!parsedData.Meeting.ScheduledStartTime.HasValue)
            parsedData.ValidationErrors.Add("Meeting start time is required");

        if (!parsedData.Meeting.ScheduledEndTime.HasValue)
            parsedData.ValidationErrors.Add("Meeting end time is required");

        if (parsedData.Meeting.ScheduledStartTime.HasValue && 
            parsedData.Meeting.ScheduledEndTime.HasValue &&
            parsedData.Meeting.ScheduledStartTime >= parsedData.Meeting.ScheduledEndTime)
        {
            parsedData.ValidationErrors.Add("Meeting end time must be after start time");
        }
    }

    public async Task<byte[]> GenerateFilledInvitationPdfAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        try
        {
            using var outputStream = new MemoryStream();
            using var writer = new PdfWriter(outputStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Add title
            document.Add(new Paragraph("INVITATION DETAILS")
                .SetFont(titleFont)
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            // Add invitation information
            AddInvitationDetails(document, invitation, titleFont, normalFont);

            document.Close();
            return await Task.FromResult(outputStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate filled invitation PDF for invitation {InvitationId}", invitation.Id);
            throw;
        }
    }

    public async Task<byte[]> GenerateInvitationSummaryPdfAsync(Invitation invitation, byte[]? qrCodeBytes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var outputStream = new MemoryStream();
            using var writer = new PdfWriter(outputStream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            var titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Add title
            document.Add(new Paragraph("VISITOR INVITATION")
                .SetFont(titleFont)
                .SetFontSize(20)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(30));

            // Add invitation summary
            AddInvitationSummary(document, invitation, titleFont, normalFont);

            // Add QR code if provided
            if (qrCodeBytes != null && qrCodeBytes.Length > 0)
            {
                AddQrCodeToDocument(document, qrCodeBytes);
            }

            document.Close();
            return await Task.FromResult(outputStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate invitation summary PDF for invitation {InvitationId}", invitation.Id);
            throw;
        }
    }

    public async Task<PdfValidationResult> ValidatePdfStructureAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var pdfReader = new PdfReader(pdfStream);
            using var pdfDoc = new PdfDocument(pdfReader);
            
            var form = PdfAcroForm.GetAcroForm(pdfDoc, false);
            
            if (form == null)
            {
                return PdfValidationResult.Success(false, new List<string>());
            }

            // TODO: Fix PDF form field retrieval based on iText7 version
            // var fieldNames = form.GetFormFields()?.Keys?.ToList() ?? new List<string>();
            var fieldNames = new List<string>(); // Placeholder until proper PDF library method is implemented
            return PdfValidationResult.Success(true, fieldNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate PDF structure");
            return PdfValidationResult.Failure(new List<string> { ex.Message });
        }
    }

    private void AddInvitationDetails(Document document, Invitation invitation, PdfFont titleFont, PdfFont normalFont)
    {
        // Add invitation details section
        document.Add(new Paragraph("INVITATION DETAILS")
            .SetFont(titleFont)
            .SetFontSize(14)
            .SetMarginBottom(10));

        var detailsTable = new Table(2);
        detailsTable.SetWidth(UnitValue.CreatePercentValue(100));

        // Add invitation details
        detailsTable.AddCell(CreateLabelValueCell("Invitation Number:", invitation.InvitationNumber, titleFont, normalFont));
        detailsTable.AddCell(CreateLabelValueCell("Visitor:", invitation.Visitor?.FullName ?? "N/A", titleFont, normalFont));
        detailsTable.AddCell(CreateLabelValueCell("Host:", invitation.Host?.FullName ?? "N/A", titleFont, normalFont));
        detailsTable.AddCell(CreateLabelValueCell("Subject:", invitation.Subject, titleFont, normalFont));
        detailsTable.AddCell(CreateLabelValueCell("Start Time:", invitation.ScheduledStartTime.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt"), titleFont, normalFont));
        detailsTable.AddCell(CreateLabelValueCell("End Time:", invitation.ScheduledEndTime.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt"), titleFont, normalFont));
        detailsTable.AddCell(CreateLabelValueCell("Status:", invitation.Status.ToString(), titleFont, normalFont));

        if (!string.IsNullOrEmpty(invitation.SpecialInstructions))
        {
            detailsTable.AddCell(CreateLabelValueCell("Special Instructions:", invitation.SpecialInstructions, titleFont, normalFont));
        }

        document.Add(detailsTable);
    }

    private void AddInvitationSummary(Document document, Invitation invitation, PdfFont titleFont, PdfFont normalFont)
    {
        // Add visitor information
        document.Add(new Paragraph($"Visitor: {invitation.Visitor?.FullName}")
            .SetFont(titleFont)
            .SetFontSize(14)
            .SetMarginBottom(5));

        document.Add(new Paragraph($"Meeting with: {invitation.Host?.FullName}")
            .SetFont(normalFont)
            .SetFontSize(12)
            .SetMarginBottom(10));

        // Add meeting details
        document.Add(new Paragraph("MEETING DETAILS")
            .SetFont(titleFont)
            .SetFontSize(12)
            .SetMarginBottom(5));

        document.Add(new Paragraph($"Subject: {invitation.Subject}")
            .SetFont(normalFont)
            .SetFontSize(10)
            .SetMarginBottom(3));

        document.Add(new Paragraph($"Date & Time: {invitation.ScheduledStartTime:dddd, MMMM dd, yyyy}")
            .SetFont(normalFont)
            .SetFontSize(10)
            .SetMarginBottom(3));

        document.Add(new Paragraph($"Time: {invitation.ScheduledStartTime:h:mm tt} - {invitation.ScheduledEndTime:h:mm tt}")
            .SetFont(normalFont)
            .SetFontSize(10)
            .SetMarginBottom(3));

        if (invitation.Location != null)
        {
            document.Add(new Paragraph($"Location: {invitation.Location.Name}")
                .SetFont(normalFont)
                .SetFontSize(10)
                .SetMarginBottom(3));
        }

        if (!string.IsNullOrEmpty(invitation.SpecialInstructions))
        {
            document.Add(new Paragraph($"Instructions: {invitation.SpecialInstructions}")
                .SetFont(normalFont)
                .SetFontSize(10)
                .SetMarginBottom(10));
        }
    }

    private void AddQrCodeToDocument(Document document, byte[] qrCodeBytes)
    {
        try
        {
            // Add QR code section
            document.Add(new Paragraph("QR CODE FOR CHECK-IN")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(12)
                .SetBold()
                .SetMarginTop(20)
                .SetMarginBottom(10));

            // Convert byte array to iText Image
            var imageData = iText.IO.Image.ImageDataFactory.Create(qrCodeBytes);
            var qrImage = new Image(imageData);
            
            // Resize QR code to appropriate size
            qrImage.SetWidth(150);
            qrImage.SetHeight(150);
            qrImage.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);

            document.Add(qrImage);

            document.Add(new Paragraph("Present this QR code at reception for quick check-in")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(10)
                .SetMarginTop(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add QR code to PDF document");
            // Add fallback text if QR code fails
            document.Add(new Paragraph("QR Code could not be generated")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(10)
                .SetMarginTop(20));
        }
    }

    private Cell CreateLabelValueCell(string label, string value, PdfFont labelFont, PdfFont valueFont)
    {
        var cell = new Cell();
        
        cell.Add(new Paragraph(label)
            .SetFont(labelFont)
            .SetFontSize(10)
            .SetMarginBottom(2));

        cell.Add(new Paragraph(value ?? "N/A")
            .SetFont(valueFont)
            .SetFontSize(10));

        cell.SetPadding(5);
        return cell;
    }
}
