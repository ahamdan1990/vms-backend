using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VisitorManagementSystem.Api.Application.Commands.Invitations;
using VisitorManagementSystem.Api.Application.Commands.Visitors;
using VisitorManagementSystem.Api.Application.Queries.Visitors;
using VisitorManagementSystem.Api.Application.Services.Email;
using VisitorManagementSystem.Api.Application.Services.Xlsx;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for XLSX operations in visitor management with dropdown support
/// </summary>
[ApiController]
[Route("api/xlsx")]
[Authorize]
public class XlsxController : BaseController
{
    private readonly IXlsxService _xlsxService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IMediator _mediator;
    private readonly ILogger<XlsxController> _logger;

    public XlsxController(
        IXlsxService xlsxService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IMediator mediator,
        ILogger<XlsxController> logger)
    {
        _xlsxService = xlsxService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _mediator = mediator;
        _logger = logger;
    }
    /// <summary>
    /// Downloads a blank XLSX invitation template with dropdowns
    /// </summary>
    /// <param name="multipleVisitors">Include sections for multiple visitors</param>
    /// <returns>XLSX template file with embedded dropdowns</returns>
    [HttpGet("invitation-template")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> DownloadInvitationTemplate([FromQuery] bool multipleVisitors = true)
    {
        try
        {
            var xlsxBytes = await _xlsxService.GenerateInvitationTemplateAsync(multipleVisitors);
            
            var fileName = multipleVisitors 
                ? "invitation-template-multiple-visitors.xlsx" 
                : "invitation-template-single-visitor.xlsx";

            return File(xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate XLSX invitation template");
            return BadRequestResponse("Failed to generate XLSX template");
        }
    }

    /// <summary>
    /// Uploads and processes a filled XLSX invitation with dropdown selections
    /// </summary>
    /// <param name="xlsxFile">Filled XLSX invitation file</param>
    /// <returns>Processing result with created invitation details</returns>
    [HttpPost("upload-invitation")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> UploadFilledInvitation(IFormFile xlsxFile)
    {
        try
        {
            // Validate file
            if (xlsxFile == null || xlsxFile.Length == 0)
            {
                return BadRequestResponse("XLSX file is required");
            }

            if (!xlsxFile.ContentType.Equals("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", StringComparison.OrdinalIgnoreCase) &&
                !xlsxFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequestResponse("Only XLSX files are allowed");
            }

            if (xlsxFile.Length > 10 * 1024 * 1024) // 10MB limit for XLSX files
            {
                return BadRequestResponse("XLSX file size cannot exceed 10MB");
            }

            // Parse the XLSX
            using var stream = xlsxFile.OpenReadStream();
            var parsedData = await _xlsxService.ParseFilledInvitationAsync(stream);

            if (!parsedData.IsValid)
            {
                return BadRequestResponse($"XLSX parsing failed: {string.Join(", ", parsedData.ValidationErrors)}");
            }

            // Process the parsed data - create visitors and invitation
            var result = await ProcessParsedInvitationData(parsedData);

            return SuccessResponse(result, "XLSX invitation processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process uploaded XLSX invitation");
            return BadRequestResponse("Failed to process XLSX invitation");
        }
    }
    /// <summary>
    /// Validates XLSX structure and required fields
    /// </summary>
    /// <param name="xlsxFile">XLSX file to validate</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    [Authorize(Policy = Permissions.Invitation.Read)]
    public async Task<IActionResult> ValidateXlsx(IFormFile xlsxFile)
    {
        try
        {
            if (xlsxFile == null || xlsxFile.Length == 0)
            {
                return BadRequestResponse("XLSX file is required");
            }

            using var stream = xlsxFile.OpenReadStream();
            var validationResult = await _xlsxService.ValidateXlsxStructureAsync(stream);

            return SuccessResponse(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate XLSX structure");
            return BadRequestResponse("Failed to validate XLSX");
        }
    }

    /// <summary>
    /// Sends XLSX invitation template via email to a host
    /// </summary>
    /// <param name="emailDto">Email details</param>
    /// <returns>Email sending result</returns>
    [HttpPost("send-template")]
    [Authorize(Policy = Permissions.Invitation.Create)]
    public async Task<IActionResult> SendXlsxTemplate([FromBody] SendXlsxTemplateDto emailDto)
    {
        try
        {
            // Generate XLSX template
            var xlsxBytes = await _xlsxService.GenerateInvitationTemplateAsync(emailDto.IncludeMultipleVisitors);

            // Get host user information
            var hostUser = new Domain.Entities.User 
            { 
                FirstName = emailDto.HostName.Split(' ').FirstOrDefault() ?? emailDto.HostName,
                LastName = emailDto.HostName.Split(' ').Skip(1).FirstOrDefault() ?? "",
                Email = new Domain.ValueObjects.Email(emailDto.HostEmail)
            };

            // Generate email content
            var emailContent = await _emailTemplateService.GenerateXlsxInvitationTemplateAsync(hostUser, emailDto.CustomMessage);

            // Create attachment
            var attachment = new Application.Services.Email.EmailAttachment
            {
                FileName = emailDto.IncludeMultipleVisitors 
                    ? "invitation-template-multiple-visitors.xlsx" 
                    : "invitation-template-single-visitor.xlsx",
                Content = xlsxBytes,
                MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };

            // Send email with XLSX attachment
            await _emailService.SendWithAttachmentsAsync(
                emailDto.HostEmail,
                "Visitor Invitation Template (Excel Format)",
                emailContent,
                new List<Application.Services.Email.EmailAttachment> { attachment });

            return SuccessResponse("XLSX template sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send XLSX template to {Email}", emailDto.HostEmail);
            return BadRequestResponse("Failed to send XLSX template");
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
                int visitorId;
                object visitorInfo;

                if (visitorData.IsExistingVisitor && visitorData.ExistingVisitorId.HasValue)
                {
                    // Use existing visitor
                    visitorId = visitorData.ExistingVisitorId.Value;
                    _logger.LogInformation("Using existing visitor with ID {VisitorId}", visitorId);
                    
                    // Get existing visitor info for response
                    var existingVisitor = await _mediator.Send(new GetVisitorByIdQuery { Id = visitorId });
                    if (existingVisitor == null)
                    {
                        throw new InvalidOperationException($"Selected existing visitor with ID {visitorId} not found in system");
                    }
                    
                    visitorInfo = new { 
                        Id = existingVisitor.Id, 
                        Name = existingVisitor.FullName,
                        Email = existingVisitor.Email,
                        IsExisting = true
                    };
                }
                else
                {
                    // Create new visitor
                    _logger.LogInformation("Creating new visitor: {FirstName} {LastName}", visitorData.FirstName, visitorData.LastName);
                    
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

                    var createdVisitor = await _mediator.Send(createVisitorCommand);
                    visitorId = createdVisitor.Id;
                    visitorInfo = new { 
                        Id = createdVisitor.Id, 
                        Name = createdVisitor.FullName,
                        Email = createdVisitor.Email,
                        IsExisting = false
                    };
                }

                // Create invitation with appropriate status based on visitor type
                var createInvitationCommand = new CreateInvitationCommand
                {
                    VisitorId = visitorId,
                    HostId = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated"),
                    Subject = parsedData.Meeting.Subject,
                    ScheduledStartTime = parsedData.Meeting.ScheduledStartTime ?? DateTime.UtcNow.AddDays(1),
                    ScheduledEndTime = parsedData.Meeting.ScheduledEndTime ?? DateTime.UtcNow.AddDays(1).AddHours(1),
                    VisitPurposeId = null, // Will need to lookup or create based on purpose text
                    LocationId = null, // Will need to lookup or create based on location text
                    SpecialInstructions = parsedData.Meeting.SpecialInstructions,
                    RequiresEscort = parsedData.Meeting.RequiresEscort,
                    RequiresBadge = parsedData.Meeting.RequiresBadge,
                    ParkingInstructions = parsedData.Meeting.ParkingInstructions,
                    CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
                };

                var createdInvitation = await _mediator.Send(createInvitationCommand);
                
                // Set invitation status based on visitor type
                InvitationStatus finalStatus;
                if (visitorData.IsExistingVisitor)
                {
                    finalStatus = InvitationStatus.Submitted; // Existing visitors go straight to submitted
                    _logger.LogInformation("Set invitation status to Submitted for existing visitor");
                }
                else
                {
                    finalStatus = InvitationStatus.Draft; // New visitors need approval first
                    _logger.LogInformation("Set invitation status to Draft for new visitor (requires approval)");
                }
                
                createdInvitations.Add(new {
                    Id = createdInvitation.Id,
                    InvitationNumber = createdInvitation.InvitationNumber,
                    VisitorName = visitorData.IsExistingVisitor ? 
                        ((dynamic)visitorInfo).Name : 
                        $"{visitorData.FirstName} {visitorData.LastName}",
                    Status = finalStatus.ToString(),
                    IsExistingVisitor = visitorData.IsExistingVisitor
                });
                
                createdVisitors.Add(visitorInfo);
            }

            return new
            {
                Success = true,
                Message = $"Successfully processed {createdVisitors.Count} visitor(s) and {createdInvitations.Count} invitation(s)",
                Summary = new 
                {
                    ExistingVisitors = createdVisitors.Count(v => ((dynamic)v).IsExisting),
                    NewVisitors = createdVisitors.Count(v => !((dynamic)v).IsExisting),
                    SubmittedInvitations = createdInvitations.Count(i => ((dynamic)i).Status == "Submitted"),
                    DraftInvitations = createdInvitations.Count(i => ((dynamic)i).Status == "Draft")
                },
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
/// DTO for sending XLSX template via email
/// </summary>
public class SendXlsxTemplateDto
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