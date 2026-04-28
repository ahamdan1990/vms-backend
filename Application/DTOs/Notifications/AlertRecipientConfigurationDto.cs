using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.DTOs.Notifications;

public class AlertRecipientConfigurationDto
{
    public int Id { get; set; }
    public NotificationAlertType AlertType { get; set; }
    public string AlertTypeLabel { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetRole { get; set; }
    public int? TargetUserId { get; set; }
    public string? TargetUserName { get; set; }
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class CreateAlertRecipientDto
{
    public NotificationAlertType AlertType { get; set; }
    public string TargetType { get; set; } = string.Empty; // "Role" | "User"
    public string? TargetRole { get; set; }
    public int? TargetUserId { get; set; }
    public string? Description { get; set; }
}

public class UpdateAlertRecipientDto
{
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
}
