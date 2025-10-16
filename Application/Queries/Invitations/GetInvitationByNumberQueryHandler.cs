using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Handler for getting invitation by number query
/// </summary>
public class GetInvitationByNumberQueryHandler : IRequestHandler<GetInvitationByNumberQuery, InvitationDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetInvitationByNumberQueryHandler> _logger;

    public GetInvitationByNumberQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetInvitationByNumberQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InvitationDto?> Handle(GetInvitationByNumberQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting invitation by number: {InvitationNumber}", request.InvitationNumber);

            var invitation = await _unitOfWork.Invitations.GetByInvitationNumberAsync(request.InvitationNumber, cancellationToken);
            
            if (invitation == null)
            {
                _logger.LogWarning("Invitation not found with number: {InvitationNumber}", request.InvitationNumber);
                return null;
            }

            var dto = _mapper.Map<InvitationDto>(invitation);
            
            _logger.LogDebug("Successfully retrieved invitation {InvitationId} by number {InvitationNumber}", 
                invitation.Id, request.InvitationNumber);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invitation by number: {InvitationNumber}", request.InvitationNumber);
            throw;
        }
    }
}
