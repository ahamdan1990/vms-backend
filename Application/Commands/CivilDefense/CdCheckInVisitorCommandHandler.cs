using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class CdCheckInVisitorCommandHandler : IRequestHandler<CdCheckInVisitorCommand, InvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CdCheckInVisitorCommandHandler> _logger;

    public CdCheckInVisitorCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CdCheckInVisitorCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InvitationDto> Handle(CdCheckInVisitorCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new InvalidOperationException($"Invitation {request.InvitationId} not found.");

        if (invitation.Status == InvitationStatus.Active)
            throw new InvalidOperationException("Visitor is already checked in.");

        var hasActiveVisit = await _unitOfWork.Invitations
            .GetByVisitorIdAndStatusAsync(invitation.VisitorId, InvitationStatus.Active, cancellationToken);
        if (hasActiveVisit.Any(v => v.Id != invitation.Id))
            throw new InvalidOperationException("Visitor is already checked in to another visit.");

        if (request.Force)
            invitation.ForceCheckIn(request.OperatorUserId);
        else
            invitation.CheckIn(request.OperatorUserId);

        _unitOfWork.Invitations.Update(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CD check-in: visitor {VisitorId} via invitation {InvitationId} by operator {Op}",
            invitation.VisitorId, invitation.Id, request.OperatorUserId);

        return _mapper.Map<InvitationDto>(invitation);
    }
}
