using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using DomainPermissions = VisitorManagementSystem.Api.Domain.Constants.Permissions;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Handler for get invitation by ID query
/// </summary>
public class GetInvitationByIdQueryHandler : IRequestHandler<GetInvitationByIdQuery, InvitationDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetInvitationByIdQueryHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetInvitationByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetInvitationByIdQueryHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
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

            // SECURITY: Check ownership permissions
            var currentUserId = GetCurrentUserId();
            var userPermissions = GetCurrentUserPermissions();

            bool hasReadAll = userPermissions.Contains(DomainPermissions.Invitation.ReadAll);
            bool hasReadOwn = userPermissions.Contains(DomainPermissions.Invitation.ReadOwn) ||
                              userPermissions.Contains(DomainPermissions.Invitation.Read);

            // If user has ONLY ReadOwn (not ReadAll), verify they own this invitation
            if (hasReadOwn && !hasReadAll && currentUserId.HasValue)
            {
                if (invitation.HostId != currentUserId.Value)
                {
                    _logger.LogWarning("User {UserId} attempted to access invitation {InvitationId} owned by {HostId}",
                        currentUserId.Value, invitation.Id, invitation.HostId);
                    return null;
                }

                _logger.LogDebug("User {UserId} has ownership of invitation {InvitationId}",
                    currentUserId.Value, invitation.Id);
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

    private int? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
            return null;

        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private List<string> GetCurrentUserPermissions()
    {
        return _httpContextAccessor.HttpContext?.User
            .FindAll("permission")
            .Select(c => c.Value)
            .ToList() ?? new List<string>();
    }
}
