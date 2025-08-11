using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Handler for get invitation by ID query
/// </summary>
public class GetInvitationByIdQueryHandler : IRequestHandler<GetInvitationByIdQuery, InvitationDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetInvitationByIdQueryHandler> _logger;

    public GetInvitationByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetInvitationByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InvitationDto?> Handle(GetInvitationByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting invitation by ID: {InvitationId}", request.Id);

            var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.Id, cancellationToken);
            
            if (invitation == null || (!request.IncludeDeleted && invitation.IsDeleted))
            {
                return null;
            }

            var invitationDto = _mapper.Map<InvitationDto>(invitation);
            return invitationDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invitation by ID: {InvitationId}", request.Id);
            throw;
        }
    }
}
