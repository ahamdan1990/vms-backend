using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.TimeSlots;

/// <summary>
/// Handler for getting bookings for a specific time slot
/// </summary>
public class GetTimeSlotBookingsQueryHandler : IRequestHandler<GetTimeSlotBookingsQuery, List<TimeSlotBookingDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetTimeSlotBookingsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<TimeSlotBookingDto>> Handle(GetTimeSlotBookingsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<TimeSlotBooking>()
            .GetQueryable()
            .Include(b => b.TimeSlot)
            .ThenInclude(ts => ts.Location)
            .Include(b => b.Invitation)
            .Include(b => b.BookedByUser)
            .Include(b => b.CancelledByUser)
            .Where(b => b.TimeSlotId == request.TimeSlotId);

        // Apply date filters if provided
        if (request.StartDate.HasValue)
        {
            query = query.Where(b => b.BookingDate >= request.StartDate.Value.Date);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(b => b.BookingDate <= request.EndDate.Value.Date);
        }

        // Order by booking date descending, then by booking time
        var bookings = await query
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.BookedOn)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<TimeSlotBookingDto>>(bookings);
    }
}
