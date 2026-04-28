using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Notifications;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Commands.Notifications;

public record CreateAlertRecipientCommand(
    NotificationAlertType AlertType,
    string TargetType,
    string? TargetRole,
    int? TargetUserId,
    string? Description,
    int CreatedBy) : IRequest<AlertRecipientConfigurationDto>;

public record UpdateAlertRecipientCommand(
    int Id,
    bool IsEnabled,
    string? Description,
    int ModifiedBy) : IRequest<AlertRecipientConfigurationDto?>;

public record DeleteAlertRecipientCommand(
    int Id,
    int DeletedBy) : IRequest<bool>;
