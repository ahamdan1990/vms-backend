using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Notifications;

public class CreateAlertRecipientCommandHandler
    : IRequestHandler<CreateAlertRecipientCommand, AlertRecipientConfigurationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateAlertRecipientCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AlertRecipientConfigurationDto> Handle(
        CreateAlertRecipientCommand request, CancellationToken cancellationToken)
    {
        AlertRecipientConfiguration config = request.TargetType == "Role"
            ? AlertRecipientConfiguration.ForRole(request.AlertType, request.TargetRole!, request.CreatedBy, request.Description)
            : AlertRecipientConfiguration.ForUser(request.AlertType, request.TargetUserId!.Value, request.CreatedBy, request.Description);

        await _unitOfWork.Repository<AlertRecipientConfiguration>().AddAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(config);
    }

    private static AlertRecipientConfigurationDto MapToDto(AlertRecipientConfiguration c) => new()
    {
        Id = c.Id,
        AlertType = c.AlertType,
        AlertTypeLabel = c.AlertType.ToString(),
        TargetType = c.TargetType,
        TargetRole = c.TargetRole,
        TargetUserId = c.TargetUserId,
        TargetUserName = c.TargetUser != null ? $"{c.TargetUser.FirstName} {c.TargetUser.LastName}" : null,
        IsEnabled = c.IsEnabled,
        Description = c.Description,
        CreatedOn = c.CreatedOn
    };
}

public class UpdateAlertRecipientCommandHandler
    : IRequestHandler<UpdateAlertRecipientCommand, AlertRecipientConfigurationDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAlertRecipientCommandHandler(IUnitOfWork unitOfWork)
        => _unitOfWork = unitOfWork;

    public async Task<AlertRecipientConfigurationDto?> Handle(
        UpdateAlertRecipientCommand request, CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.Repository<AlertRecipientConfiguration>()
            .GetByIdAsync(request.Id, cancellationToken);

        if (config == null) return null;

        config.IsEnabled = request.IsEnabled;
        config.Description = request.Description;
        config.UpdateModifiedBy(request.ModifiedBy);

        _unitOfWork.Repository<AlertRecipientConfiguration>().Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AlertRecipientConfigurationDto
        {
            Id = config.Id,
            AlertType = config.AlertType,
            AlertTypeLabel = config.AlertType.ToString(),
            TargetType = config.TargetType,
            TargetRole = config.TargetRole,
            TargetUserId = config.TargetUserId,
            IsEnabled = config.IsEnabled,
            Description = config.Description,
            CreatedOn = config.CreatedOn
        };
    }
}

public class DeleteAlertRecipientCommandHandler
    : IRequestHandler<DeleteAlertRecipientCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAlertRecipientCommandHandler(IUnitOfWork unitOfWork)
        => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(
        DeleteAlertRecipientCommand request, CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.Repository<AlertRecipientConfiguration>()
            .GetByIdAsync(request.Id, cancellationToken);

        if (config == null) return false;

        config.Deactivate(request.DeletedBy);
        _unitOfWork.Repository<AlertRecipientConfiguration>().Update(config);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
