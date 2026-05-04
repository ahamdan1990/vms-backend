using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class CdStaffCheckOutCommandHandler : IRequestHandler<CdStaffCheckOutCommand, CdStaffAttendanceDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CdStaffCheckOutCommandHandler> _logger;

    public CdStaffCheckOutCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CdStaffCheckOutCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CdStaffAttendanceDto> Handle(CdStaffCheckOutCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User {request.UserId} not found.");

        var repo = _unitOfWork.Repository<StaffAttendance>();

        var attendance = await repo.FirstOrDefaultAsync(
            a => a.UserId == request.UserId && a.CheckOutTime == null, cancellationToken)
            ?? throw new InvalidOperationException("No open check-in found for this staff member.");

        attendance.RecordCheckOut(request.ProcessedByUserId);
        if (request.CameraId.HasValue)
            attendance.CameraId = request.CameraId;

        repo.Update(attendance);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CD: staff check-out user {UserId} attendance {AttId}",
            request.UserId, attendance.Id);

        return new CdStaffAttendanceDto
        {
            Id               = attendance.Id,
            UserId           = attendance.UserId,
            UserFullName     = user.FullName,
            CheckInTime      = attendance.CheckInTime,
            CheckOutTime     = attendance.CheckOutTime,
            Method           = attendance.Method,
            CameraId         = attendance.CameraId,
            Notes            = attendance.Notes,
            IsCurrentlyInside = attendance.IsCurrentlyInside,
            Duration         = attendance.Duration,
        };
    }
}
