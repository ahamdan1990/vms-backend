using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Application.Services.Common;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class GetCdPersonReportQuery : IRequest<CdPersonReportDto?>
{
    public string PersonType { get; set; } = "Visitor";
    public int PersonId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class GetCdPersonReportQueryHandler
    : IRequestHandler<GetCdPersonReportQuery, CdPersonReportDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUrlResolverService _urlResolver;

    public GetCdPersonReportQueryHandler(IUnitOfWork unitOfWork, IUrlResolverService urlResolver)
    {
        _unitOfWork = unitOfWork;
        _urlResolver = urlResolver;
    }

    public async Task<CdPersonReportDto?> Handle(
        GetCdPersonReportQuery request,
        CancellationToken cancellationToken)
    {
        var isVisitor = string.Equals(request.PersonType, "Visitor", StringComparison.OrdinalIgnoreCase);

        PersonSummaryDto? person;
        List<PersonVisitRecordDto> records;

        if (isVisitor)
        {
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.PersonId, cancellationToken);
            if (visitor == null) return null;

            person = new PersonSummaryDto
            {
                Id = visitor.Id,
                FullName = visitor.FullName,
                Phone = visitor.PhoneNumber?.Value,
                Company = visitor.Company,
                ProfilePhotoUrl = string.IsNullOrWhiteSpace(visitor.ProfilePhotoPath)
                    ? null : _urlResolver.GetAbsoluteUrl(visitor.ProfilePhotoPath),
                PersonType = "Visitor",
                IsVip = visitor.IsVip,
                IsBlacklisted = visitor.IsBlacklisted,
            };

            var invQuery = _unitOfWork.Invitations
                .GetQueryable()
                .AsNoTracking()
                .Where(i => i.VisitorId == request.PersonId && !i.IsDeleted);

            if (request.StartDate.HasValue)
                invQuery = invQuery.Where(i =>
                    (i.CheckedInAt ?? i.ScheduledStartTime) >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                invQuery = invQuery.Where(i =>
                    (i.CheckedInAt ?? i.ScheduledStartTime) <= request.EndDate.Value);

            var invRaw = await invQuery
                .OrderByDescending(i => i.CheckedInAt ?? i.ScheduledStartTime)
                .Take(100)
                .Select(i => new
                {
                    i.Id,
                    i.CheckedInAt,
                    i.CheckedOutAt,
                    Status = i.Status.ToString(),
                    LocationName = i.Location != null ? i.Location.Name : null,
                    HostName = i.Host != null ? (i.Host.FirstName + " " + i.Host.LastName).Trim() : null,
                })
                .ToListAsync(cancellationToken);

            records = invRaw.Select(i => new PersonVisitRecordDto
            {
                Id = i.Id,
                CheckedInAt = i.CheckedInAt,
                CheckedOutAt = i.CheckedOutAt,
                Status = i.Status,
                Location = i.LocationName,
                Host = i.HostName,
            }).ToList();
        }
        else
        {
            var user = await _unitOfWork.Users.GetByIdAsync(request.PersonId, cancellationToken);
            if (user == null) return null;

            person = new PersonSummaryDto
            {
                Id = user.Id,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Phone = user.PhoneNumber?.Value,
                Department = user.Department,
                JobTitle = user.JobTitle,
                ProfilePhotoUrl = string.IsNullOrWhiteSpace(user.ProfilePhotoPath)
                    ? null : _urlResolver.GetAbsoluteUrl(user.ProfilePhotoPath),
                PersonType = "Staff",
            };

            var presQuery = _unitOfWork.Repository<StaffPresence>()
                .GetQueryable()
                .AsNoTracking()
                .Where(sp => sp.UserId == request.PersonId);

            if (request.StartDate.HasValue)
                presQuery = presQuery.Where(sp => sp.CheckedInAt >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                presQuery = presQuery.Where(sp => sp.CheckedInAt <= request.EndDate.Value);

            var presRaw = await presQuery
                .OrderByDescending(sp => sp.CheckedInAt)
                .Take(100)
                .Select(sp => new
                {
                    sp.Id,
                    CheckedInAt = (DateTime?)sp.CheckedInAt,
                    sp.CheckedOutAt,
                    Status = sp.Status.ToString(),
                    LocationName = sp.Location != null ? sp.Location.Name : null,
                    sp.Notes,
                })
                .ToListAsync(cancellationToken);

            records = presRaw.Select(sp => new PersonVisitRecordDto
            {
                Id = sp.Id,
                CheckedInAt = sp.CheckedInAt,
                CheckedOutAt = sp.CheckedOutAt,
                Status = sp.Status,
                Location = sp.LocationName,
                Notes = sp.Notes,
            }).ToList();
        }

        // Face events — both visitor and staff
        var feQuery = _unitOfWork.Repository<CameraFaceEvent>()
            .GetQueryable()
            .AsNoTracking()
            .Where(e => e.IsKnown &&
                        e.PersonId == request.PersonId &&
                        e.PersonType == request.PersonType);

        if (request.StartDate.HasValue)
            feQuery = feQuery.Where(e => e.CapturedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            feQuery = feQuery.Where(e => e.CapturedAt <= request.EndDate.Value);

        var feRaw = await feQuery
            .OrderByDescending(e => e.CapturedAt)
            .Take(50)
            .Select(e => new
            {
                e.Id,
                e.CapturedAt,
                e.SnapshotPath,
                e.Similarity,
                CameraName = e.Camera != null ? e.Camera.Name : null,
            })
            .ToListAsync(cancellationToken);

        var faceEvents = feRaw.Select(e => new PersonFaceEventDto
        {
            Id = e.Id,
            CapturedAt = e.CapturedAt,
            SnapshotUrl = string.IsNullOrWhiteSpace(e.SnapshotPath)
                ? null : _urlResolver.GetAbsoluteUrl(e.SnapshotPath),
            Similarity = e.Similarity.HasValue ? (double?)e.Similarity.Value : null,
            CameraName = e.CameraName,
        }).ToList();

        return new CdPersonReportDto
        {
            Person = person,
            Records = records,
            FaceEvents = faceEvents,
        };
    }
}
