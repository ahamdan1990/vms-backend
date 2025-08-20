using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;
using VisitorManagementSystem.Api.Application.Queries.TimeSlots;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.TimeSlots;

/// <summary>
/// Handler for getting a time slot by ID
/// </summary>
public class GetTimeSlotByIdQueryHandler : IRequestHandler<GetTimeSlotByIdQuery, TimeSlotDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetTimeSlotByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TimeSlotDto?> Handle(GetTimeSlotByIdQuery request, CancellationToken cancellationToken)
    {
        var timeSlot = await _unitOfWork.Repository<TimeSlot>()
            .GetQueryable()
            .Include(ts => ts.Location)
            .FirstOrDefaultAsync(ts => ts.Id == request.Id, cancellationToken);

        return timeSlot == null ? null : _mapper.Map<TimeSlotDto>(timeSlot);
    }
}