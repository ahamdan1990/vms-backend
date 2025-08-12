using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.Services.Email;
using VisitorManagementSystem.Api.Application.Services.Csv;
using VisitorManagementSystem.Api.Domain.Constants;
using MediatR;
using VisitorManagementSystem.Api.Application.Commands.Visitors;
using VisitorManagementSystem.Api.Application.Commands.Invitations;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for CSV operations in visitor management
/// </summary>
[ApiController]
[Route("api/csv")]
[Authorize]
public class CsvController : BaseController
{
    private readonly ICsvService _csvService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IMediator _mediator;
    private readonly ILogger<CsvController> _logger;

    public CsvController(
        ICsvService csvService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IMediator mediator,
        ILogger<CsvController> logger)
    {
        _csvService = csvService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _mediator = mediator;
        _logger = logger;
    }
    /// <summary>
    /// Downloads a blank CSV invitation template with reference sheets
    /// </summary>
    /// <param name="multipleVisitors">Include sections for multiple visitors</param>
    /// <returns>ZIP file containing CSV template and reference sheets</returns>
    [HttpGet("invitation-template")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> DownloadInvitationTemplate([FromQuery] bool multipleVisitors = true)
    {
        try
        {
            var zipBytes = await _csvService.GenerateInvitationTemplateAsync(multipleVisitors);
            
            var fileName = multipleVisitors 
                ? "invitation-template-multiple-visitors.zip" 
                : "invitation-template-single-visitor.zip";

            return File(zipBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate CSV invitation template");
            return BadRequestResponse("Failed to generate CSV template");
        }
    }

    /// <summary>
    /// Uploads and processes a filled CSV invitation
    /// </summary>
    /// <param name="csvFile">Filled CSV invitation file</param>
    /// <returns>Processing result with created invitation details</returns>
    [HttpPost("upload-invitation")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> UploadFilledInvitation(IFormFile csvFile)
    {
        try
        {
            // Validate file
            if (csvFile == null || csvFile.Length == 0)
            {
                return BadRequestResponse("CSV file is required");
            }

            if (!csvFile.ContentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase) &&
                !csvFile.ContentType.Equals("application/csv", StringComparison.OrdinalIgnoreCase) &&
                !csvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequestResponse("Only CSV files are allowed");
            }
            if (csvFile.Length > 5 * 1024 * 1024) // 5MB limit (CSV files are typically much smaller)
            {
                return BadRequestResponse("CSV file size cannot exceed 5MB");
            }

            // Parse the CSV
            using var stream = csvFile.OpenReadStream();
            var parsedData = await _csvService.ParseFilledInvitationAsync(stream);

            if (!parsedData.IsValid)
            {
                return BadRequestResponse($"CSV parsing failed: {string.Join(", ", parsedData.ValidationErrors)}");
            }

            // Process the parsed data - create visitors and invitation
            var result = await ProcessParsedInvitationData(parsedData);

            return SuccessResponse(result, "CSV invitation processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process uploaded CSV invitation");
            return BadRequestResponse("Failed to process CSV invitation");
        }
    }

    /// <summary>
    /// Validates CSV structure and required fields
    /// </summary>
    /// <param name="csvFile">CSV file to validate</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    [Authorize(Policy = Permissions.Invitation.Read)]
    public async Task<IActionResult> ValidateCsv(IFormFile csvFile)
    {
        try
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                return BadRequestResponse("CSV file is required");
            }

            using var stream = csvFile.OpenReadStream();
            var validationResult = await _csvService.ValidateCsvStructureAsync(stream);

            return SuccessResponse(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate CSV structure");
            return BadRequestResponse("Failed to validate CSV");
        }
    }
    /// <summary>
    /// Sends CSV invitation template via email to a host
    /// </summary>
    /// <param name="emailDto">Email details</param>
    /// <returns>Email sending result</returns>
    [HttpPost("send-template")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> SendCsvTemplate([FromBody] SendCsvTemplateDto emailDto)
    {
        try
        {
            // Generate enhanced CSV template with reference sheets
            var zipBytes = await _csvService.GenerateInvitationTemplateAsync(emailDto.IncludeMultipleVisitors);

            // Get host user information (simplified - in production, validate user exists)
            var hostUser = new Domain.Entities.User 
            { 
                FirstName = emailDto.HostName.Split(' ').FirstOrDefault() ?? emailDto.HostName,
                LastName = emailDto.HostName.Split(' ').Skip(1).FirstOrDefault() ?? "",
                Email = new Domain.ValueObjects.Email(emailDto.HostEmail)
            };

            // Generate email content
            var emailContent = await _emailTemplateService.GenerateCsvInvitationTemplateAsync(hostUser, emailDto.CustomMessage);

            // Create attachment
            var attachment = new Application.Services.Email.EmailAttachment
            {
                FileName = emailDto.IncludeMultipleVisitors 
                    ? "invitation-template-multiple-visitors.zip" 
                    : "invitation-template-single-visitor.zip",
                Content = zipBytes,
                MimeType = "application/zip"
            };

            // Send email with ZIP attachment
            await _emailService.SendWithAttachmentsAsync(
                emailDto.HostEmail,
                "Visitor Invitation Template (Enhanced CSV Format)",
                emailContent,
                new List<Application.Services.Email.EmailAttachment> { attachment });

            return SuccessResponse("Enhanced CSV template sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send CSV template to {Email}", emailDto.HostEmail);
            return BadRequestResponse("Failed to send CSV template");
        }
    }
    private async Task<object> ProcessParsedInvitationData(Application.Services.Pdf.ParsedInvitationData parsedData)
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
                    EmergencyContacts = new List<Application.DTOs.Visitors.CreateEmergencyContactDto>(),
                    CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
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
/// DTO for sending CSV template via email
/// </summary>
public class SendCsvTemplateDto
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