using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class CdCheckOutVisitorCommandHandler : IRequestHandler<CdCheckOutVisitorCommand, InvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CdCheckOutVisitorCommandHandler> _logger;

    public CdCheckOutVisitorCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CdCheckOutVisitorCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InvitationDto> Handle(CdCheckOutVisitorCommand request, CancellationToken cancellationToken)
    {
        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new InvalidOperationException($"Invitation {request.InvitationId} not found.");

        if (invitation.Status != InvitationStatus.Active)
            throw new InvalidOperationException($"Cannot check out invitation in status '{invitation.Status}'.");

        invitation.CheckOut(request.OperatorUserId);
        _unitOfWork.Invitations.Update(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CD check-out: visitor {VisitorId} via invitation {InvitationId} by operator {Op}",
            invitation.VisitorId, invitation.Id, request.OperatorUserId);

        return _mapper.Map<InvitationDto>(invitation);
    }
}
