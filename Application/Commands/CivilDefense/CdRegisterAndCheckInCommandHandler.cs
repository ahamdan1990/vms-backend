using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class CdRegisterAndCheckInCommandHandler
    : IRequestHandler<CdRegisterAndCheckInCommand, CdRegisterAndCheckInResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CdRegisterAndCheckInCommandHandler> _logger;

    public CdRegisterAndCheckInCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CdRegisterAndCheckInCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<CdRegisterAndCheckInResultDto> Handle(
        CdRegisterAndCheckInCommand request, CancellationToken cancellationToken)
    {
        // Locate visitor by phone, falling back to synthetic email lookup
        var visitor = await _unitOfWork.Visitors.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        bool wasExisting = visitor != null;

        if (visitor == null)
        {
            var cleanPhone = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());
            var syntheticEmail = $"{cleanPhone}@cd.local";

            visitor = new Visitor
            {
                FirstName        = request.FirstName.Trim(),
                LastName         = request.LastName.Trim(),
                Email            = new Email(syntheticEmail),
                NormalizedEmail  = syntheticEmail.ToUpperInvariant(),
                PhoneNumber      = new PhoneNumber(request.PhoneNumber),
                GovernmentId     = request.NationalId,
                GovernmentIdType = request.NationalId != null ? "NationalId" : null,
                Nationality      = request.Nationality,
                IsCivilian       = true,
                CivilianOrigin   = request.CivilianOrigin,
                Language         = "ar",
                IsActive         = true,
            };
            visitor.SetCreatedBy(request.OperatorUserId);
            await _unitOfWork.Visitors.AddAsync(visitor, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("CD: registered new visitor {VisitorId} ({Name})",
                visitor.Id, visitor.FullName);
        }

        var now = DateTime.UtcNow;
        var invitation = new Invitation
        {
            VisitorId           = visitor.Id,
            HostId              = request.OperatorUserId,
            Type                = InvitationType.WalkIn,
            Status              = InvitationStatus.Approved,
            Subject             = "Civil Defense Walk-In",
            InvitationNumber    = Invitation.GenerateInvitationNumber(),
            ScheduledStartTime  = now,
            ScheduledEndTime    = now.AddHours(8),
            RequiresApproval    = false,
            RequiresBadge       = false,
        };
        invitation.SetCreatedBy(request.OperatorUserId);
        await _unitOfWork.Invitations.AddAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        invitation.CheckIn(request.OperatorUserId);
        _unitOfWork.Invitations.Update(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CD: checked in visitor {VisitorId} via invitation {InvId}",
            visitor.Id, invitation.Id);

        return new CdRegisterAndCheckInResultDto
        {
            Visitor           = _mapper.Map<VisitorDto>(visitor),
            Invitation        = _mapper.Map<InvitationDto>(invitation),
            WasExistingVisitor = wasExisting,
        };
    }
}
