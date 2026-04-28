using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Notifications;

public record GetAlertRecipientsQuery(NotificationAlertType? AlertType = null)
    : IRequest<List<AlertRecipientConfigurationDto>>;

public class GetAlertRecipientsQueryHandler
    : IRequestHandler<GetAlertRecipientsQuery, List<AlertRecipientConfigurationDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAlertRecipientsQueryHandler(IUnitOfWork unitOfWork)
        => _unitOfWork = unitOfWork;

    public async Task<List<AlertRecipientConfigurationDto>> Handle(
        GetAlertRecipientsQuery request, CancellationToken cancellationToken)
    {
        var configs = await _unitOfWork.Repository<AlertRecipientConfiguration>()
            .GetAllAsync(
                predicate: request.AlertType.HasValue
                    ? c => c.AlertType == request.AlertType.Value
                    : null,
                orderBy: q => q.OrderBy(c => c.AlertType).ThenBy(c => c.TargetType),
                include: "TargetUser",
                cancellationToken: cancellationToken);

        return configs.Select(c => new AlertRecipientConfigurationDto
        {
            Id = c.Id,
            AlertType = c.AlertType,
            AlertTypeLabel = c.AlertType.ToString(),
            TargetType = c.TargetType,
            TargetRole = c.TargetRole,
            TargetUserId = c.TargetUserId,
            TargetUserName = c.TargetUser != null
                ? $"{c.TargetUser.FirstName} {c.TargetUser.LastName}"
                : null,
            IsEnabled = c.IsEnabled,
            Description = c.Description,
            CreatedOn = c.CreatedOn
        }).ToList();
    }
}
