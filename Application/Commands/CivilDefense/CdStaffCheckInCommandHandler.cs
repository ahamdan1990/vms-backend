using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class CdStaffCheckInCommandHandler : IRequestHandler<CdStaffCheckInCommand, CdStaffAttendanceDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CdStaffCheckInCommandHandler> _logger;

    public CdStaffCheckInCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CdStaffCheckInCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CdStaffAttendanceDto> Handle(CdStaffCheckInCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"User {request.UserId} not found.");

        var repo = _unitOfWork.Repository<StaffAttendance>();

        var open = await repo.FirstOrDefaultAsync(
            a => a.UserId == request.UserId && a.CheckOutTime == null, cancellationToken);
        if (open != null)
            throw new InvalidOperationException("Staff member is already checked in.");

        var attendance = new StaffAttendance
        {
            UserId      = request.UserId,
            CheckInTime = DateTime.UtcNow,
            Method      = request.Method,
            CameraId    = request.CameraId,
            Notes       = request.Notes,
            IsActive    = true,
            CreatedOn   = DateTime.UtcNow,
        };
        attendance.SetCreatedBy(request.ProcessedByUserId);

        await repo.AddAsync(attendance, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CD: staff check-in user {UserId} attendance {AttId} method {Method}",
            request.UserId, attendance.Id, request.Method);

        return MapToDto(attendance, user.FullName);
    }

    private static CdStaffAttendanceDto MapToDto(StaffAttendance a, string userName) => new()
    {
        Id               = a.Id,
        UserId           = a.UserId,
        UserFullName     = userName,
        CheckInTime      = a.CheckInTime,
        CheckOutTime     = a.CheckOutTime,
        Method           = a.Method,
        CameraId         = a.CameraId,
        Notes            = a.Notes,
        IsCurrentlyInside = a.IsCurrentlyInside,
        Duration         = a.Duration,
    };
}
