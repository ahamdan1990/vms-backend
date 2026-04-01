using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.Services.Common;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Invitations;

/// <summary>
/// Handler for receptionist/dashboard completed-today visitor list.
/// </summary>
public class GetCompletedTodayInvitationsQueryHandler : IRequestHandler<GetCompletedTodayInvitationsQuery, List<InvitationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ISystemTimeZoneService _systemTimeZoneService;
    private readonly ILogger<GetCompletedTodayInvitationsQueryHandler> _logger;

    public GetCompletedTodayInvitationsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ISystemTimeZoneService systemTimeZoneService,
        ILogger<GetCompletedTodayInvitationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _systemTimeZoneService = systemTimeZoneService ?? throw new ArgumentNullException(nameof(systemTimeZoneService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<InvitationDto>> Handle(GetCompletedTodayInvitationsQuery request, CancellationToken cancellationToken)
    {
        var dayRange = await _systemTimeZoneService.GetCurrentDayUtcRangeAsync(cancellationToken);

        var query = _unitOfWork.Invitations
            .GetQueryable()
            .Include(i => i.Visitor)
            .Include(i => i.Host)
            .Include(i => i.VisitPurpose)
            .Include(i => i.Location)
            .Where(i =>
                !i.IsDeleted &&
                i.Status == InvitationStatus.Completed &&
                i.CheckedInAt != null &&
                i.CheckedOutAt != null &&
                i.CheckedInAt >= dayRange.StartUtc &&
                i.CheckedInAt < dayRange.EndUtcExclusive &&
                i.CheckedOutAt >= dayRange.StartUtc &&
                i.CheckedOutAt < dayRange.EndUtcExclusive);

        if (request.LocationId.HasValue)
        {
            query = query.Where(i => i.LocationId == request.LocationId.Value);
        }

        var invitations = await query
            .AsNoTracking()
            .OrderByDescending(i => i.CheckedOutAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} completed-today invitations for dashboard (LocationId: {LocationId})",
            invitations.Count,
            request.LocationId);

        return _mapper.Map<List<InvitationDto>>(invitations);
    }
}
