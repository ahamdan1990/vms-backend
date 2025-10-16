using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Application.Services.QrCode;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Handler for approve invitation command
/// </summary>
public class ApproveInvitationCommandHandler : IRequestHandler<ApproveInvitationCommand, InvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IQrCodeService _qrCodeService;
    private readonly ILogger<ApproveInvitationCommandHandler> _logger;
    private readonly INotificationService _notificationService;

    public ApproveInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IQrCodeService qrCodeService,
        ILogger<ApproveInvitationCommandHandler> logger,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _qrCodeService = qrCodeService;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<InvitationDto> Handle(ApproveInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing approve invitation command for invitation: {InvitationId}", request.InvitationId);

            // Get existing invitation
            var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken);
            if (invitation == null)
            {
                throw new InvalidOperationException($"Invitation with ID '{request.InvitationId}' not found.");
            }

            // Validate approver exists
            var approver = await _unitOfWork.Users.GetByIdAsync(request.ApprovedBy, cancellationToken);
            if (approver == null)
            {
                throw new InvalidOperationException($"Approver with ID '{request.ApprovedBy}' not found.");
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Approve the invitation
                invitation.Approve(request.ApprovedBy, request.Comments);
                _unitOfWork.Invitations.Update(invitation);

                // Create approval event
                var approvalEvent = InvitationEvent.Create(
                    invitation.Id,
                    InvitationEventTypes.Approved,
                    $"Invitation approved by {approver.FullName}",
                    request.ApprovedBy,
                    request.Comments
                );
                await _unitOfWork.Repository<InvitationEvent>().AddAsync(approvalEvent, cancellationToken);

                // Generate QR code for the invitation
                var qrCodeData = await _qrCodeService.GenerateInvitationQrDataAsync(invitation, cancellationToken);
                invitation.UpdateQrCode(qrCodeData);

                // Create QR code generation event
                var qrEvent = InvitationEvent.Create(
                    invitation.Id,
                    InvitationEventTypes.QrCodeGenerated,
                    "QR code generated for approved invitation",
                    request.ApprovedBy
                );
                await _unitOfWork.Repository<InvitationEvent>().AddAsync(qrEvent, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Invitation approved successfully: {InvitationId} by {ApprovedBy}",
                    request.InvitationId, request.ApprovedBy);

                // ?? NOTIFICATION: Notify host of approval
                await _notificationService.NotifyInvitationApprovalAsync(
                    invitation.Id, invitation.HostId, approved: true, request.Comments, cancellationToken);


                // Return updated invitation DTO
                var updatedInvitation = await _unitOfWork.Invitations.GetByIdAsync(invitation.Id, cancellationToken);
                var invitationDto = _mapper.Map<InvitationDto>(updatedInvitation);
                return invitationDto;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving invitation: {InvitationId}", request.InvitationId);
            throw;
        }
    }
}
