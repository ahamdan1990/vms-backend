using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class StaffCheckOutCommand : IRequest<bool>
{
    public int StaffPresenceId { get; set; }
    public int CheckedOutById { get; set; }
}

public class StaffCheckOutCommandHandler : IRequestHandler<StaffCheckOutCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StaffCheckOutCommandHandler> _logger;

    public StaffCheckOutCommandHandler(IUnitOfWork unitOfWork, ILogger<StaffCheckOutCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(StaffCheckOutCommand request, CancellationToken cancellationToken)
    {
        var presence = await _unitOfWork.Repository<StaffPresence>()
            .GetQueryable()
            .FirstOrDefaultAsync(sp => sp.Id == request.StaffPresenceId, cancellationToken)
            ?? throw new KeyNotFoundException($"Staff presence record {request.StaffPresenceId} not found.");

        if (presence.Status != StaffPresenceStatus.Active)
            throw new InvalidOperationException("This staff member is not currently checked in.");

        presence.Status = StaffPresenceStatus.CheckedOut;
        presence.CheckedOutAt = DateTime.UtcNow;
        presence.CheckedOutById = request.CheckedOutById;

        _unitOfWork.Repository<StaffPresence>().Update(presence);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Staff presence {PresenceId} checked out by {OperatorId}",
            request.StaffPresenceId, request.CheckedOutById);
        return true;
    }
}
