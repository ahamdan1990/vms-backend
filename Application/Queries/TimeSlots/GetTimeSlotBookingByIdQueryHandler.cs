using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.TimeSlots;

/// <summary>
/// Handler for getting a time slot booking by ID
/// </summary>
public class GetTimeSlotBookingByIdQueryHandler : IRequestHandler<GetTimeSlotBookingByIdQuery, TimeSlotBookingDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetTimeSlotBookingByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TimeSlotBookingDto?> Handle(GetTimeSlotBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await _unitOfWork.Repository<TimeSlotBooking>()
            .GetQueryable()
            .Include(b => b.TimeSlot)
            .ThenInclude(ts => ts.Location)
            .Include(b => b.Invitation)
            .Include(b => b.BookedByUser)
            .Include(b => b.CancelledByUser)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        return booking == null ? null : _mapper.Map<TimeSlotBookingDto>(booking);
    }
}
