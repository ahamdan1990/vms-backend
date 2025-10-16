using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.Commands.TimeSlots;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.TimeSlots;

/// <summary>
/// Handler for updating a time slot
/// </summary>
public class UpdateTimeSlotCommandHandler : IRequestHandler<UpdateTimeSlotCommand, TimeSlotDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateTimeSlotCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TimeSlotDto> Handle(UpdateTimeSlotCommand request, CancellationToken cancellationToken)
    {
        // Get existing time slot
        var timeSlot = await _unitOfWork.Repository<TimeSlot>().GetByIdAsync(request.Id);
        if (timeSlot == null)
        {
            throw new ArgumentException($"Time slot with ID {request.Id} not found");
        }

        // Validate time slot
        if (request.StartTime >= request.EndTime)
        {
            throw new ArgumentException("Start time must be before end time");
        }

        // Update properties
        timeSlot.Name = request.Name.Trim();
        timeSlot.StartTime = request.StartTime;
        timeSlot.EndTime = request.EndTime;
        timeSlot.MaxVisitors = request.MaxVisitors;
        timeSlot.ActiveDays = request.ActiveDays.Trim();
        timeSlot.LocationId = request.LocationId;
        timeSlot.BufferMinutes = request.BufferMinutes;
        timeSlot.DisplayOrder = request.DisplayOrder;
        timeSlot.IsActive = request.IsActive;

        timeSlot.ModifiedBy=request.ModifiedBy;

        // Validate business rules
        var validationErrors = timeSlot.ValidateTimeSlot();
        if (validationErrors.Any())
        {
            throw new ArgumentException($"Validation failed: {string.Join(", ", validationErrors)}");
        }

        // Save changes
        _unitOfWork.Repository<TimeSlot>().Update(timeSlot);
        await _unitOfWork.SaveChangesAsync();

        // Return mapped DTO
        return _mapper.Map<TimeSlotDto>(timeSlot);
    }
}