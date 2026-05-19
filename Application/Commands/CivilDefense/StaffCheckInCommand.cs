using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class StaffCheckInCommand : IRequest<int>
{
    public int UserId { get; set; }
    public int? LocationId { get; set; }
    public string? Notes { get; set; }
    public int CheckedInById { get; set; }
}

public class StaffCheckInCommandHandler : IRequestHandler<StaffCheckInCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StaffCheckInCommandHandler> _logger;

    public StaffCheckInCommandHandler(IUnitOfWork unitOfWork, ILogger<StaffCheckInCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> Handle(StaffCheckInCommand request, CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        // Block duplicate active check-in
        var existing = await _unitOfWork.Repository<StaffPresence>()
            .GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                sp => sp.UserId == request.UserId && sp.Status == StaffPresenceStatus.Active,
                cancellationToken);

        if (existing != null)
            throw new InvalidOperationException($"{user.FullName} is already checked in.");

        var presence = new StaffPresence
        {
            UserId = request.UserId,
            CheckedInAt = DateTime.UtcNow,
            Status = StaffPresenceStatus.Active,
            LocationId = request.LocationId,
            CheckedInById = request.CheckedInById,
            Notes = request.Notes?.Trim()
        };

        await _unitOfWork.Repository<StaffPresence>().AddAsync(presence, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Staff {UserId} checked in by {OperatorId}", request.UserId, request.CheckedInById);
        return presence.Id;
    }
}
