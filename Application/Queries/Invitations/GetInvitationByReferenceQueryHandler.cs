using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Handler for GetInvitationByReferenceQuery
/// </summary>
public class GetInvitationByReferenceQueryHandler : IRequestHandler<GetInvitationByReferenceQuery, InvitationDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetInvitationByReferenceQueryHandler> _logger;

    public GetInvitationByReferenceQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetInvitationByReferenceQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InvitationDto?> Handle(GetInvitationByReferenceQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting invitation by reference: {Reference}", request.Reference);

        // Find invitation by reference (ID, invitation number, or QR code)
        var invitation = await FindInvitationByReferenceAsync(request.Reference, cancellationToken);

        if (invitation == null)
        {
            _logger.LogDebug("Invitation not found with reference: {Reference}", request.Reference);
            return null;
        }

        // Map to DTO
        var invitationDto = _mapper.Map<InvitationDto>(invitation);

        _logger.LogDebug("Found invitation: {InvitationId} ({InvitationNumber})", invitation.Id, invitation.InvitationNumber);

        return invitationDto;
    }

    private async Task<Invitation?> FindInvitationByReferenceAsync(string reference, CancellationToken cancellationToken)
    {
        // Try to parse as invitation ID first
        if (int.TryParse(reference, out var invitationId))
        {
            var invitationById = await _unitOfWork.Invitations.GetByIdAsync(invitationId, cancellationToken);
            if (invitationById != null)
                return invitationById;
        }

        // Try to find by invitation number
        var invitationByNumber = await _unitOfWork.Invitations.GetByInvitationNumberAsync(reference, cancellationToken);
        if (invitationByNumber != null)
            return invitationByNumber;

        // Try to find by QR code data
        var invitationByQr = await _unitOfWork.Invitations.GetByQrCodeAsync(reference, cancellationToken);
        if (invitationByQr != null)
            return invitationByQr;

        return null;
    }
}
