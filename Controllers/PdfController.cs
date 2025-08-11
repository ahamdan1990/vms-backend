using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.Services.Email;
using VisitorManagementSystem.Api.Application.Services.Pdf;
using VisitorManagementSystem.Api.Domain.Constants;
using MediatR;
using VisitorManagementSystem.Api.Application.Commands.Visitors;
using VisitorManagementSystem.Api.Application.Commands.Invitations;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for PDF operations in visitor management
/// </summary>
[ApiController]
[Route("api/pdf")]
[Authorize]
public class PdfController : BaseController
{
    private readonly IPdfService _pdfService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IMediator _mediator;
    private readonly ILogger<PdfController> _logger;

    public PdfController(
        IPdfService pdfService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IMediator mediator,
        ILogger<PdfController> logger)
    {
        _pdfService = pdfService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Downloads a blank PDF invitation template
    /// </summary>
    /// <param name="multipleVisitors">Include sections for multiple visitors</param>
    /// <returns>PDF template file</returns>
    [HttpGet("invitation-template")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> DownloadInvitationTemplate([FromQuery] bool multipleVisitors = true)
    {
        try
        {
            var pdfBytes = await _pdfService.GenerateInvitationTemplateAsync(multipleVisitors);
            
            var fileName = multipleVisitors 
                ? "invitation-template-multiple-visitors.pdf" 
                : "invitation-template-single-visitor.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF invitation template");
            return BadRequestResponse("Failed to generate PDF template");
        }
    }

    /// <summary>
    /// Uploads and processes a filled PDF invitation
    /// </summary>
    /// <param name="pdfFile">Filled PDF invitation file</param>
    /// <returns>Processing result with created invitation details</returns>
    [HttpPost("upload-invitation")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> UploadFilledInvitation(IFormFile pdfFile)
    {
        try
        {
            // Validate file
            if (pdfFile == null || pdfFile.Length == 0)
            {
                return BadRequestResponse("PDF file is required");
            }

            if (!pdfFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequestResponse("Only PDF files are allowed");
            }

            if (pdfFile.Length > 10 * 1024 * 1024) // 10MB limit
            {
                return BadRequestResponse("PDF file size cannot exceed 10MB");
            }

            // Parse the PDF
            using var stream = pdfFile.OpenReadStream();
            var parsedData = await _pdfService.ParseFilledInvitationAsync(stream);

            if (!parsedData.IsValid)
            {
                return BadRequestResponse($"PDF parsing failed: {string.Join(", ", parsedData.ValidationErrors)}");
            }

            // Process the parsed data - create visitors and invitation
            var result = await ProcessParsedInvitationData(parsedData);

            return SuccessResponse(result, "PDF invitation processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process uploaded PDF invitation");
            return BadRequestResponse("Failed to process PDF invitation");
        }
    }

    /// <summary>
    /// Validates PDF structure and form fields
    /// </summary>
    /// <param name="pdfFile">PDF file to validate</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    [Authorize(Policy = Permissions.Invitation.Read)]
    public async Task<IActionResult> ValidatePdf(IFormFile pdfFile)
    {
        try
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                return BadRequestResponse("PDF file is required");
            }

            using var stream = pdfFile.OpenReadStream();
            var validationResult = await _pdfService.ValidatePdfStructureAsync(stream);

            return SuccessResponse(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate PDF structure");
            return BadRequestResponse("Failed to validate PDF");
        }
    }

    /// <summary>
    /// Sends PDF invitation template via email to a host
    /// </summary>
    /// <param name="emailDto">Email details</param>
    /// <returns>Email sending result</returns>
    [HttpPost("send-template")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> SendPdfTemplate([FromBody] SendPdfTemplateDto emailDto)
    {
        try
        {
            // Generate PDF template
            var pdfBytes = await _pdfService.GenerateInvitationTemplateAsync(emailDto.IncludeMultipleVisitors);

            // Get host user information (simplified - in production, validate user exists)
            var hostUser = new Domain.Entities.User 
            { 
                FirstName = emailDto.HostName.Split(' ').FirstOrDefault() ?? emailDto.HostName,
                LastName = emailDto.HostName.Split(' ').Skip(1).FirstOrDefault() ?? "",
                Email = new Domain.ValueObjects.Email(emailDto.HostEmail)
            };

            // Generate email content
            var emailContent = await _emailTemplateService.GeneratePdfInvitationTemplateAsync(hostUser);

            // Create attachment
            var attachment = new Application.Services.Email.EmailAttachment
            {
                FileName = emailDto.IncludeMultipleVisitors 
                    ? "invitation-template-multiple-visitors.pdf" 
                    : "invitation-template-single-visitor.pdf",
                Content = pdfBytes,
                MimeType = "application/pdf"
            };

            // Send email with PDF attachment
            await _emailService.SendWithAttachmentsAsync(
                emailDto.HostEmail,
                "Visitor Invitation Template",
                emailContent,
                new List<Application.Services.Email.EmailAttachment> { attachment });

            return SuccessResponse("PDF template sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send PDF template to {Email}", emailDto.HostEmail);
            return BadRequestResponse("Failed to send PDF template");
        }
    }
    private async Task<object> ProcessParsedInvitationData(ParsedInvitationData parsedData)
    {
        var createdVisitors = new List<object>();
        var createdInvitations = new List<object>();

        try
        {
            // Process each visitor
            foreach (var visitorData in parsedData.Visitors)
            {
                // Create visitor command
                var createVisitorCommand = new CreateVisitorCommand
                {
                    FirstName = visitorData.FirstName,
                    LastName = visitorData.LastName,
                    Email = visitorData.Email,
                    PhoneNumber = visitorData.PhoneNumber,
                    Company = visitorData.Company,
                    GovernmentId = visitorData.GovernmentId,
                    Nationality = visitorData.Nationality,
                    EmergencyContacts = new List<Application.DTOs.Visitors.CreateEmergencyContactDto>()
                };

                // Add emergency contact if available
                if (visitorData.EmergencyContact != null)
                {
                    createVisitorCommand.EmergencyContacts.Add(new Application.DTOs.Visitors.CreateEmergencyContactDto
                    {
                        FirstName = visitorData.EmergencyContact.FirstName,
                        LastName = visitorData.EmergencyContact.LastName,
                        PhoneNumber = visitorData.EmergencyContact.PhoneNumber,
                        Relationship = visitorData.EmergencyContact.Relationship
                    });
                }

                // Create visitor
                var createdVisitor = await _mediator.Send(createVisitorCommand);
                createdVisitors.Add(new { 
                    Id = createdVisitor.Id, 
                    Name = createdVisitor.FullName,
                    Email = createdVisitor.Email 
                });

                // Create invitation for this visitor
                var createInvitationCommand = new CreateInvitationCommand
                {
                    VisitorId = createdVisitor.Id,
                    HostId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated"),
                    Subject = parsedData.Meeting.Subject,
                    ScheduledStartTime = parsedData.Meeting.ScheduledStartTime ?? DateTime.UtcNow.AddDays(1),
                    ScheduledEndTime = parsedData.Meeting.ScheduledEndTime ?? DateTime.UtcNow.AddDays(1).AddHours(1),
                    VisitPurposeId = null, // Will need to lookup or create based on purpose text
                    LocationId = null, // Will need to lookup or create based on location text
                    SpecialInstructions = parsedData.Meeting.SpecialInstructions,
                    RequiresEscort = parsedData.Meeting.RequiresEscort,
                    RequiresBadge = parsedData.Meeting.RequiresBadge,
                    ParkingInstructions = parsedData.Meeting.ParkingInstructions
                };

                var createdInvitation = await _mediator.Send(createInvitationCommand);
                createdInvitations.Add(new {
                    Id = createdInvitation.Id,
                    InvitationNumber = createdInvitation.InvitationNumber,
                    VisitorName = createdVisitor.FullName
                });
            }

            return new
            {
                Success = true,
                Message = $"Successfully processed {createdVisitors.Count} visitor(s) and {createdInvitations.Count} invitation(s)",
                Visitors = createdVisitors,
                Invitations = createdInvitations,
                HostInfo = new
                {
                    parsedData.Host.FullName,
                    parsedData.Host.Email,
                    parsedData.Host.Department
                },
                MeetingInfo = new
                {
                    parsedData.Meeting.Subject,
                    parsedData.Meeting.ScheduledStartTime,
                    parsedData.Meeting.ScheduledEndTime,
                    parsedData.Meeting.Location
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process parsed invitation data");
            throw;
        }
    }
}

/// <summary>
/// DTO for sending PDF template via email
/// </summary>
public class SendPdfTemplateDto
{
    /// <summary>
    /// Host name
    /// </summary>
    [Required]
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// Host email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string HostEmail { get; set; } = string.Empty;

    /// <summary>
    /// Include multiple visitor sections
    /// </summary>
    public bool IncludeMultipleVisitors { get; set; } = true;

    /// <summary>
    /// Custom message to include in email
    /// </summary>
    [MaxLength(500)]
    public string? CustomMessage { get; set; }
}
