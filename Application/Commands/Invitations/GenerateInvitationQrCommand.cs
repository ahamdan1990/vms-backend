using MediatR;
using VisitorManagementSystem.Api.Application.Services.QrCode;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

public record GenerateInvitationQrCommand(int InvitationId) : IRequest<GenerateInvitationQrResult>;

public record GenerateInvitationQrResult(bool IsSuccess, string? QrCode, string? Error);

public class GenerateInvitationQrCommandHandler : IRequestHandler<GenerateInvitationQrCommand, GenerateInvitationQrResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQrCodeService _qrCodeService;
    private readonly ILogger<GenerateInvitationQrCommandHandler> _logger;

    public GenerateInvitationQrCommandHandler(
        IUnitOfWork unitOfWork,
        IQrCodeService qrCodeService,
        ILogger<GenerateInvitationQrCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    public async Task<GenerateInvitationQrResult> Handle(GenerateInvitationQrCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken);

        if (invitation == null)
            return new GenerateInvitationQrResult(false, null, "Invitation not found");

        // Only generate for statuses where a QR still has operational value
        if (invitation.Status != InvitationStatus.Approved && invitation.Status != InvitationStatus.Active)
            return new GenerateInvitationQrResult(false, null, "QR code is not applicable for this invitation status");

        // Return existing QR if already generated (idempotent)
        if (!string.IsNullOrEmpty(invitation.QrCode))
            return new GenerateInvitationQrResult(true, invitation.QrCode, null);

        var qrData = await _qrCodeService.GenerateInvitationQrDataAsync(invitation, cancellationToken);
        invitation.UpdateQrCode(qrData);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Lazily generated QR code for invitation {InvitationId} (status: {Status})", invitation.Id, invitation.Status);

        return new GenerateInvitationQrResult(true, qrData, null);
    }
}
