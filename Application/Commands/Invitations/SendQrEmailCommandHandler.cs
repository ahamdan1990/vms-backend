using MediatR;
using VisitorManagementSystem.Api.Application.Commands.Invitations;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.Services.Email;
using VisitorManagementSystem.Api.Application.Services.QrCode;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

public class SendQrEmailCommandHandler
    : IRequestHandler<SendQrEmailCommand, ApiResponseDto<object>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQrCodeService _qrCodeService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<SendQrEmailCommandHandler> _logger;

    public SendQrEmailCommandHandler(
        IUnitOfWork unitOfWork,
        IQrCodeService qrCodeService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILogger<SendQrEmailCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _qrCodeService = qrCodeService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _logger = logger;
    }

    public async Task<ApiResponseDto<object>> Handle(SendQrEmailCommand request, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Processing SendQrEmailCommand for invitation ID: {InvitationId}", request.InvitationId);

            var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, ct);

            if (invitation == null)
            {
                _logger.LogWarning("Invitation not found: {InvitationId}", request.InvitationId);
                return ApiResponseDto<object>.ErrorResponse("Invitation not found.");
            }

            if (string.IsNullOrEmpty(invitation.QrCode))
            {
                _logger.LogWarning("QR code not generated for invitation: {InvitationId}", request.InvitationId);
                return ApiResponseDto<object>.ErrorResponse("QR code not generated for this invitation.");
            }

            if (invitation.Visitor == null || string.IsNullOrEmpty(invitation.Visitor.Email))
            {
                _logger.LogWarning("Visitor information missing for invitation: {InvitationId}", request.InvitationId);
                return ApiResponseDto<object>.ErrorResponse("Visitor information is missing.");
            }

            // Generate QR image
            var qrImageBytes = await _qrCodeService.GenerateQrCodeImageAsync(invitation.QrCode, 300);

            // Create attachment
            var attachment = new EmailAttachment
            {
                FileName = $"invitation-{invitation.InvitationNumber}-qr.png",
                Content = qrImageBytes,
                MimeType = "image/png"
            };

            // Generate email content
            var emailContent = _emailTemplateService.GenerateQrInvitationTemplate(
                invitation.Visitor.FullName,
                invitation.Subject,
                invitation.ScheduledStartTime,
                invitation.Location);

            // Send email
            await _emailService.SendWithAttachmentsAsync(
                invitation.Visitor.Email,
                $"Your Visit QR Code - {invitation.Subject}",
                emailContent,
                new[] { attachment },
                ct);

            _logger.LogInformation("QR code sent successfully for invitation: {InvitationId}", request.InvitationId);

            return ApiResponseDto<object>.SuccessResponse(null, "QR code sent successfully to visitor's email");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send QR code for invitation: {InvitationId}", request.InvitationId);
            return ApiResponseDto<object>.ErrorResponse("Failed to send QR code: " + ex.Message);
        }
    }
}
