using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Application.Services.Common;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class LookupVisitorByPhoneQuery : IRequest<VisitorLookupDto?>
{
    public string Phone { get; set; } = string.Empty;
}

public class LookupVisitorByPhoneQueryHandler
    : IRequestHandler<LookupVisitorByPhoneQuery, VisitorLookupDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUrlResolverService _urlResolver;

    public LookupVisitorByPhoneQueryHandler(IUnitOfWork unitOfWork, IUrlResolverService urlResolver)
    {
        _unitOfWork = unitOfWork;
        _urlResolver = urlResolver;
    }

    public async Task<VisitorLookupDto?> Handle(
        LookupVisitorByPhoneQuery request,
        CancellationToken cancellationToken)
    {
        var visitor = await _unitOfWork.Visitors.GetByPhoneNumberAsync(request.Phone, cancellationToken);
        if (visitor == null)
            return null;

        // Check if currently inside
        var activeInvitation = await _unitOfWork.Invitations
            .GetQueryable()
            .AsNoTracking()
            .Where(i =>
                i.VisitorId == visitor.Id &&
                i.Status == InvitationStatus.Active &&
                !i.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        return new VisitorLookupDto
        {
            VisitorId = visitor.Id,
            FirstName = visitor.FirstName,
            LastName = visitor.LastName,
            FullName = visitor.FullName,
            Phone = visitor.PhoneNumber?.Value,
            Company = visitor.Company,
            ProfilePhotoUrl = string.IsNullOrWhiteSpace(visitor.ProfilePhotoPath) ? null : _urlResolver.GetAbsoluteUrl(visitor.ProfilePhotoPath),
            IsCurrentlyInside = activeInvitation != null,
            ActiveInvitationId = activeInvitation?.Id,
            IsVip = visitor.IsVip,
            IsBlacklisted = visitor.IsBlacklisted,
            BlacklistReason = visitor.BlacklistReason
        };
    }
}
