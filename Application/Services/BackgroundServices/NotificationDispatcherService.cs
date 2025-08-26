using VisitorManagementSystem.Api.Application.Services.Email;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.Interfaces.Services;

namespace VisitorManagementSystem.Api.Application.Services.BackgroundServices;

/// <summary>
/// Background service that processes notification escalation rules and external delivery
/// Handles unacknowledged alerts, escalation logic, and email/SMS notifications
/// </summary>
public class NotificationDispatcherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDispatcherService> _logger;
    private readonly IConfiguration _configuration;

    public NotificationDispatcherService(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationDispatcherService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Dispatcher Service started");

        // Wait for application startup
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationEscalationAsync(stoppingToken);
                await ProcessExternalNotificationQueueAsync(stoppingToken);
                await CleanupExpiredAlertsAsync(stoppingToken);
                
                // Process every 30 seconds (configurable)
                var processingInterval = _configuration.GetValue<int>("NotificationDispatcher:IntervalSeconds", 30);
                await Task.Delay(TimeSpan.FromSeconds(processingInterval), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Notification Dispatcher Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Notification Dispatcher Service");
                
                // Wait before retrying
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        _logger.LogInformation("Notification Dispatcher Service stopped");
    }

    /// <summary>
    /// Process escalation rules for unacknowledged alerts
    /// </summary>
    private async Task ProcessNotificationEscalationAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5); // Check alerts older than 5 minutes

            // Get unacknowledged alerts that may need escalation
            var unacknowledgedAlerts = await unitOfWork.Repository<NotificationAlert>()
                .GetAllAsync(
                    a => !a.IsAcknowledged && 
                         a.IsActive && 
                         a.CreatedOn < cutoffTime && 
                         (a.ExpiresOn == null || a.ExpiresOn > DateTime.UtcNow),
                    orderBy: q => q.OrderBy(r => r.CreatedOn),
                    cancellationToken: cancellationToken);

            if (!unacknowledgedAlerts.Any())
                return;

            // Get all active escalation rules
            var escalationRules = await unitOfWork.Repository<AlertEscalation>()
                .GetAllAsync(
                    rule => rule.IsEnabled && rule.IsActive,
                    orderBy: q => q.OrderBy(r => r.RulePriority),
                    cancellationToken: cancellationToken);

            foreach (var alert in unacknowledgedAlerts)
            {
                await ProcessAlertEscalation(alert, escalationRules, unitOfWork, notificationService, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification escalation");
            throw;
        }
    }

    /// <summary>
    /// Process escalation for a specific alert
    /// </summary>
    private async Task ProcessAlertEscalation(NotificationAlert alert, IEnumerable<AlertEscalation> escalationRules,
        IUnitOfWork unitOfWork, INotificationService notificationService, CancellationToken cancellationToken)
    {
        try
        {
            var minutesSinceCreated = (DateTime.UtcNow - alert.CreatedOn).TotalMinutes;

            // Find applicable escalation rules
            var applicableRules = escalationRules
                .Where(rule => rule.MatchesAlert(alert) && minutesSinceCreated >= rule.EscalationDelayMinutes)
                .Take(1); // Apply first matching rule only

            foreach (var rule in applicableRules)
            {
                await ExecuteEscalationAction(rule, alert, unitOfWork, notificationService, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing escalation for alert {AlertId}", alert.Id);
        }
    }

    /// <summary>
    /// Execute the escalation action
    /// </summary>
    private async Task ExecuteEscalationAction(AlertEscalation rule, NotificationAlert alert,
        IUnitOfWork unitOfWork, INotificationService notificationService, CancellationToken cancellationToken)
    {
        try
        {
            switch (rule.Action)
            {
                case EscalationAction.EscalateToRole:
                    if (!string.IsNullOrEmpty(rule.EscalationTargetRole))
                    {
                        await notificationService.NotifyRoleAsync(
                            rule.EscalationTargetRole,
                            $"ESCALATED: {alert.Title}",
                            $"Alert escalated due to no acknowledgment. Original: {alert.Message}",
                            alert.Type,
                            AlertPriority.High,
                            cancellationToken: cancellationToken);
                    }
                    break;

                case EscalationAction.EscalateToUser:
                    if (rule.EscalationTargetUserId.HasValue)
                    {
                        await notificationService.NotifyUserAsync(
                            rule.EscalationTargetUserId.Value,
                            $"ESCALATED: {alert.Title}",
                            $"Alert escalated to you. Original: {alert.Message}",
                            alert.Type,
                            AlertPriority.High,
                            cancellationToken: cancellationToken);
                    }
                    break;

                case EscalationAction.SendEmail:
                    await SendEscalationEmail(rule, alert, cancellationToken);
                    break;

                case EscalationAction.SendSMS:
                    await SendEscalationSMS(rule, alert, cancellationToken);
                    break;

                case EscalationAction.CreateHighPriorityAlert:
                    var highPriorityAlert = NotificationAlert.CreateFRAlert(
                        $"HIGH PRIORITY: {alert.Title}",
                        $"Escalated alert: {alert.Message}",
                        alert.Type,
                        AlertPriority.Critical,
                        targetRole: rule.EscalationTargetRole);

                    await unitOfWork.Repository<NotificationAlert>().AddAsync(highPriorityAlert, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    await notificationService.SendBulkNotificationAsync(highPriorityAlert, cancellationToken);
                    break;

                case EscalationAction.LogCriticalEvent:
                    _logger.LogCritical(
                        "ESCALATED ALERT: {AlertTitle} (ID: {AlertId}) - {AlertMessage}",
                        alert.Title, alert.Id, alert.Message);
                    break;
            }

            _logger.LogInformation(
                "Escalation action {Action} executed for alert {AlertId} using rule {RuleName}",
                rule.Action, alert.Id, rule.RuleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing escalation action {Action} for alert {AlertId}",
                rule.Action, alert.Id);
        }
    }

    /// <summary>
    /// Send escalation email
    /// </summary>
    private async Task SendEscalationEmail(AlertEscalation rule, NotificationAlert alert, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var emailService = scope.ServiceProvider.GetService<IEmailService>();
        
        if (emailService == null || string.IsNullOrEmpty(rule.EscalationEmails))
            return;

        try
        {
            var emails = rule.EscalationEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            foreach (var email in emails)
            {
                await emailService.SendAlertEmailAsync(
                    email,
                    $"ESCALATED ALERT: {alert.Title}",
                    $"Alert Details:\n\nTitle: {alert.Title}\nMessage: {alert.Message}\nPriority: {alert.Priority}\nTime: {alert.CreatedOn}\n\nThis alert was escalated due to lack of acknowledgment.",
                    cancellationToken);
            }

            _logger.LogInformation("Escalation emails sent for alert {AlertId} to {EmailCount} recipients",
                alert.Id, emails.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending escalation email for alert {AlertId}", alert.Id);
        }
    }

    /// <summary>
    /// Send escalation SMS
    /// </summary>
    private async Task SendEscalationSMS(AlertEscalation rule, NotificationAlert alert, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var smsService = scope.ServiceProvider.GetService<ISMSService>();
        
        if (smsService == null || string.IsNullOrEmpty(rule.EscalationPhones))
            return;

        try
        {
            var phones = rule.EscalationPhones.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            var smsMessage = $"ALERT: {alert.Title} - {alert.Message.Substring(0, Math.Min(alert.Message.Length, 100))}...";

            foreach (var phone in phones)
            {
                await smsService.SendSMSAsync(phone, smsMessage, cancellationToken);
            }

            _logger.LogInformation("Escalation SMS sent for alert {AlertId} to {PhoneCount} recipients",
                alert.Id, phones.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending escalation SMS for alert {AlertId}", alert.Id);
        }
    }

    /// <summary>
    /// Process queue of notifications that need external delivery
    /// </summary>
    private async Task ProcessExternalNotificationQueueAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            // Get high-priority alerts that haven't been sent externally
            var alertsToSendExternally = await unitOfWork.Repository<NotificationAlert>()
                .GetAllAsync(
                    a => !a.SentExternally && 
                         a.IsActive && 
                         (a.Priority == AlertPriority.Critical || a.Priority == AlertPriority.Emergency) &&
                         a.CreatedOn > DateTime.UtcNow.AddHours(-24), // Only process recent alerts
                    orderBy: q => q.OrderBy(a => a.CreatedOn),
                    take: 10,
                    cancellationToken: cancellationToken);

            foreach (var alert in alertsToSendExternally)
            {
                await ProcessExternalNotification(alert, unitOfWork, cancellationToken);
                alert.MarkSentExternally();
                unitOfWork.Repository<NotificationAlert>().Update(alert);
            }

            if (alertsToSendExternally.Any())
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing external notification queue");
            throw;
        }
    }

    /// <summary>
    /// Process external notification delivery
    /// </summary>
    private async Task ProcessExternalNotification(NotificationAlert alert, IUnitOfWork unitOfWork, 
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var emailService = scope.ServiceProvider.GetService<IEmailService>();
        
        try
        {
            // For critical and emergency alerts, send email notifications
            if (emailService != null && (alert.Priority == AlertPriority.Critical || alert.Priority == AlertPriority.Emergency))
            {
                // Get admin users for critical alerts
                var adminUsers = await unitOfWork.Users.GetByRoleAsync(UserRole.Administrator, cancellationToken);
                
                foreach (var admin in adminUsers.Where(u => !string.IsNullOrEmpty(u.Email)))
                {
                    await emailService.SendAlertEmailAsync(
                        admin.Email!,
                        $"CRITICAL ALERT: {alert.Title}",
                        $"Priority: {alert.Priority}\nTime: {alert.CreatedOn}\nMessage: {alert.Message}",
                        cancellationToken);
                }

                _logger.LogInformation("External notifications sent for critical alert {AlertId}", alert.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending external notification for alert {AlertId}", alert.Id);
        }
    }

    /// <summary>
    /// Clean up expired alerts
    /// </summary>
    private async Task CleanupExpiredAlertsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var expiredAlerts = await unitOfWork.Repository<NotificationAlert>()
                .GetAllAsync(
                    a => a.ExpiresOn.HasValue && a.ExpiresOn < DateTime.UtcNow && a.IsActive,
                    take: 100,
                    cancellationToken: cancellationToken);

            if (expiredAlerts.Any())
            {
                foreach (var alert in expiredAlerts)
                {
                    alert.Deactivate();
                    unitOfWork.Repository<NotificationAlert>().Update(alert);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Cleaned up {Count} expired alerts", expiredAlerts.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired alerts");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Notification Dispatcher Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
