using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.Commands.TimeSlots;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.TimeSlots;

/// <summary>
/// Handler for creating a time slot
/// </summary>
public class CreateTimeSlotCommandHandler : IRequestHandler<CreateTimeSlotCommand, TimeSlotDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateTimeSlotCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TimeSlotDto> Handle(CreateTimeSlotCommand request, CancellationToken cancellationToken)
    {
        // Validate time slot
        if (request.StartTime >= request.EndTime)
        {
            throw new ArgumentException("Start time must be before end time");
        }

        // Create entity
        var timeSlot = new TimeSlot
        {
            Name = request.Name.Trim(),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            MaxVisitors = request.MaxVisitors,
            ActiveDays = request.ActiveDays.Trim(),
            LocationId = request.LocationId,
            BufferMinutes = request.BufferMinutes,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        timeSlot.SetCreatedBy(request.CreatedBy);

        // Validate business rules
        var validationErrors = timeSlot.ValidateTimeSlot();
        if (validationErrors.Any())
        {
            throw new ArgumentException($"Validation failed: {string.Join(", ", validationErrors)}");
        }

        // Save to database
        await _unitOfWork.Repository<TimeSlot>().AddAsync(timeSlot);
        await _unitOfWork.SaveChangesAsync();

        // Return mapped DTO
        return _mapper.Map<TimeSlotDto>(timeSlot);
    }
}