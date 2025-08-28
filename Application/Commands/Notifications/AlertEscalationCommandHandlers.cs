using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Notifications;

/// <summary>
/// Handler for creating alert escalation rules
/// </summary>
public class CreateAlertEscalationCommandHandler : IRequestHandler<CreateAlertEscalationCommand, AlertEscalationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAlertEscalationCommandHandler> _logger;

    public CreateAlertEscalationCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateAlertEscalationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AlertEscalationDto> Handle(CreateAlertEscalationCommand request, CancellationToken cancellationToken)
    {
        var escalation = new AlertEscalation
        {
            RuleName = request.RuleName.Trim(),
            AlertType = request.AlertType,
            AlertPriority = request.AlertPriority,
            TargetRole = request.TargetRole?.Trim(),
            LocationId = request.LocationId,
            EscalationDelayMinutes = request.EscalationDelayMinutes,
            Action = request.Action,
            EscalationTargetRole = request.EscalationTargetRole?.Trim(),
            EscalationTargetUserId = request.EscalationTargetUserId,
            EscalationEmails = request.EscalationEmails?.Trim(),
            EscalationPhones = request.EscalationPhones?.Trim(),
            MaxAttempts = request.MaxAttempts,
            IsEnabled = request.IsEnabled,
            RulePriority = request.RulePriority,
            Configuration = request.Configuration?.Trim(),
            CreatedBy = request.CreatedBy,
            CreatedOn = DateTime.UtcNow
        };

        await _unitOfWork.Repository<AlertEscalation>().AddAsync(escalation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alert escalation rule created: {RuleName} for {AlertType}/{Priority} by user {UserId}", 
            escalation.RuleName, escalation.AlertType, escalation.AlertPriority, request.CreatedBy);

        // Load related entities for DTO
        Location? location = null;
        if (escalation.LocationId.HasValue)
        {
            location = await _unitOfWork.Repository<Location>().GetByIdAsync(escalation.LocationId.Value, cancellationToken);
        }

        User? targetUser = null;
        if (escalation.EscalationTargetUserId.HasValue)
        {
            targetUser = await _unitOfWork.Users.GetByIdAsync(escalation.EscalationTargetUserId.Value, cancellationToken);
        }

        return MapToDto(escalation, location, targetUser);
    }

    private static AlertEscalationDto MapToDto(AlertEscalation escalation, Location? location, User? targetUser)
    {
        return new AlertEscalationDto
        {
            Id = escalation.Id,
            RuleName = escalation.RuleName,
            AlertType = escalation.AlertType,
            AlertPriority = escalation.AlertPriority,
            TargetRole = escalation.TargetRole,
            LocationId = escalation.LocationId,
            LocationName = location?.Name,
            EscalationDelayMinutes = escalation.EscalationDelayMinutes,
            Action = escalation.Action,
            EscalationTargetRole = escalation.EscalationTargetRole,
            EscalationTargetUserId = escalation.EscalationTargetUserId,
            EscalationTargetUserName = targetUser?.FullName,
            EscalationEmails = escalation.EscalationEmails,
            EscalationPhones = escalation.EscalationPhones,
            MaxAttempts = escalation.MaxAttempts,
            IsEnabled = escalation.IsEnabled,
            RulePriority = escalation.RulePriority,
            Configuration = escalation.Configuration,
            CreatedOn = escalation.CreatedOn,
            ModifiedOn = escalation.ModifiedOn,
            IsActive = escalation.IsActive
        };
    }
}

/// <summary>
/// Handler for updating alert escalation rules
/// </summary>
public class UpdateAlertEscalationCommandHandler : IRequestHandler<UpdateAlertEscalationCommand, AlertEscalationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAlertEscalationCommandHandler> _logger;

    public UpdateAlertEscalationCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateAlertEscalationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AlertEscalationDto> Handle(UpdateAlertEscalationCommand request, CancellationToken cancellationToken)
    {
        var escalation = await _unitOfWork.Repository<AlertEscalation>()
            .GetByIdAsync(request.Id, cancellationToken);

        if (escalation == null)
            throw new KeyNotFoundException($"Alert escalation rule with ID {request.Id} not found");

        // Update properties
        escalation.RuleName = request.RuleName.Trim();
        escalation.TargetRole = request.TargetRole?.Trim();
        escalation.LocationId = request.LocationId;
        escalation.EscalationDelayMinutes = request.EscalationDelayMinutes;
        escalation.Action = request.Action;
        escalation.EscalationTargetRole = request.EscalationTargetRole?.Trim();
        escalation.EscalationTargetUserId = request.EscalationTargetUserId;
        escalation.EscalationEmails = request.EscalationEmails?.Trim();
        escalation.EscalationPhones = request.EscalationPhones?.Trim();
        escalation.MaxAttempts = request.MaxAttempts;
        escalation.IsEnabled = request.IsEnabled;
        escalation.RulePriority = request.RulePriority;
        escalation.Configuration = request.Configuration?.Trim();
        escalation.ModifiedBy = request.ModifiedBy;
        escalation.ModifiedOn = DateTime.UtcNow;

        _unitOfWork.Repository<AlertEscalation>().Update(escalation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alert escalation rule updated: {RuleName} (ID: {Id}) by user {UserId}", 
            escalation.RuleName, escalation.Id, request.ModifiedBy);

        // Load related entities for DTO
        Location? location = null;
        if (escalation.LocationId.HasValue)
        {
            location = await _unitOfWork.Repository<Location>().GetByIdAsync(escalation.LocationId.Value, cancellationToken);
        }

        User? targetUser = null;
        if (escalation.EscalationTargetUserId.HasValue)
        {
            targetUser = await _unitOfWork.Users.GetByIdAsync(escalation.EscalationTargetUserId.Value, cancellationToken);
        }

        return MapToDto(escalation, location, targetUser);
    }

    private static AlertEscalationDto MapToDto(AlertEscalation escalation, Location? location, User? targetUser)
    {
        return new AlertEscalationDto
        {
            Id = escalation.Id,
            RuleName = escalation.RuleName,
            AlertType = escalation.AlertType,
            AlertPriority = escalation.AlertPriority,
            TargetRole = escalation.TargetRole,
            LocationId = escalation.LocationId,
            LocationName = location?.Name,
            EscalationDelayMinutes = escalation.EscalationDelayMinutes,
            Action = escalation.Action,
            EscalationTargetRole = escalation.EscalationTargetRole,
            EscalationTargetUserId = escalation.EscalationTargetUserId,
            EscalationTargetUserName = targetUser?.FullName,
            EscalationEmails = escalation.EscalationEmails,
            EscalationPhones = escalation.EscalationPhones,
            MaxAttempts = escalation.MaxAttempts,
            IsEnabled = escalation.IsEnabled,
            RulePriority = escalation.RulePriority,
            Configuration = escalation.Configuration,
            CreatedOn = escalation.CreatedOn,
            ModifiedOn = escalation.ModifiedOn,
            IsActive = escalation.IsActive
        };
    }
}

/// <summary>
/// Handler for deleting alert escalation rules
/// </summary>
public class DeleteAlertEscalationCommandHandler : IRequestHandler<DeleteAlertEscalationCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteAlertEscalationCommandHandler> _logger;

    public DeleteAlertEscalationCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteAlertEscalationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteAlertEscalationCommand request, CancellationToken cancellationToken)
    {
        var escalation = await _unitOfWork.Repository<AlertEscalation>()
            .GetByIdAsync(request.Id, cancellationToken);

        if (escalation == null)
            throw new KeyNotFoundException($"Alert escalation rule with ID {request.Id} not found");

        // Soft delete
        escalation.IsActive = false;
        escalation.ModifiedBy = request.DeletedBy;
        escalation.ModifiedOn = DateTime.UtcNow;

        _unitOfWork.Repository<AlertEscalation>().Update(escalation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Alert escalation rule deleted: {RuleName} (ID: {Id}) by user {UserId}", 
            escalation.RuleName, escalation.Id, request.DeletedBy);

        return true;
    }
}
