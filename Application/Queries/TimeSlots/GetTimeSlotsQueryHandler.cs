using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;
using VisitorManagementSystem.Api.Application.Queries.Auth;
using VisitorManagementSystem.Api.Application.Queries.TimeSlots;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.TimeSlots;

/// <summary>
/// Handler for getting paginated time slots
/// </summary>
public class GetTimeSlotsQueryHandler : IRequestHandler<GetTimeSlotsQuery, PagedResultDto<TimeSlotDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTimeSlotsQueryHandler> _logger;

    public GetTimeSlotsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetTimeSlotsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<TimeSlotDto>> Handle(GetTimeSlotsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<TimeSlot>().GetQueryable();

        // Apply filters
        if (request.ActiveOnly)
        {
            query = query.Where(ts => ts.IsActive);
        }

        if (request.LocationId.HasValue)
        {
            query = query.Where(ts => ts.LocationId == null || ts.LocationId == request.LocationId.Value);
        }

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "name" => request.SortDirection.ToLower() == "desc" 
                ? query.OrderByDescending(ts => ts.Name)
                : query.OrderBy(ts => ts.Name),
            "starttime" => request.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(ts => ts.StartTime)
                : query.OrderBy(ts => ts.StartTime),
            _ => request.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(ts => ts.DisplayOrder).ThenByDescending(ts => ts.StartTime)
                : query.OrderBy(ts => ts.DisplayOrder).ThenBy(ts => ts.StartTime)
        };

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var timeSlots = await query
            .Include(ts => ts.Location)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var timeSlotDtos = _mapper.Map<List<TimeSlotDto>>(timeSlots);

        return new PagedResultDto<TimeSlotDto>
        {
            Items = timeSlotDtos,
            TotalCount = totalCount,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}