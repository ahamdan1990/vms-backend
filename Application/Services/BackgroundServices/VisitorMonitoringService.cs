using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.BackgroundServices;

/// <summary>
/// Background service that monitors visitor attendance and sends notifications for delays and no-shows
/// </summary>
public class VisitorMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VisitorMonitoringService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes
    private readonly int _delayThresholdMinutes = 15; // Notify if visitor is 15+ minutes late
    private readonly int _noShowThresholdMinutes = 30; // Mark as no-show after 30+ minutes

    public VisitorMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<VisitorMonitoringService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Visitor Monitoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorVisitorsAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Visitor Monitoring Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while monitoring visitors");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait before retrying
            }
        }

        _logger.LogInformation("Visitor Monitoring Service stopped");
    }

    private async Task MonitorVisitorsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var delayCheckTime = now.AddMinutes(-_delayThresholdMinutes);
        var noShowCheckTime = now.AddMinutes(-_noShowThresholdMinutes);

        try
        {
            // Get approved invitations that should have started but haven't been checked in
            var relevantInvitations = await unitOfWork.Invitations.GetByDateRangeAsync(
                now.AddHours(-2),
                now,
                cancellationToken);

            var approvedNotCheckedIn = relevantInvitations?
                .Where(i => i.Status == InvitationStatus.Approved && !i.IsDeleted)
                .ToList() ?? new List<Domain.Entities.Invitation>();

            foreach (var invitation in approvedNotCheckedIn)
            {
                if (invitation.ScheduledStartTime > now)
                {
                    continue; // Not yet time for the visit
                }

                var minutesLate = (int)(now - invitation.ScheduledStartTime).TotalMinutes;

                // Check if this is a no-show case (30+ minutes late)
                if (minutesLate >= _noShowThresholdMinutes)
                {
                    // Check if we've already sent a no-show notification
                    var existingAlerts = await unitOfWork.NotificationAlerts.GetAllAsync(cancellationToken);
                    var hasNoShowAlert = existingAlerts?.Any(n =>
                        n.RelatedEntityType == "Invitation" &&
                        n.RelatedEntityId == invitation.Id &&
                        n.Type == NotificationAlertType.VisitorNoShow) ?? false;

                    if (!hasNoShowAlert && invitation.Visitor != null)
                    {
                        // Send no-show notification
                        await notificationService.NotifyVisitorNoShowAsync(
                            invitation.Id,
                            invitation.VisitorId,
                            $"{invitation.Visitor.FirstName} {invitation.Visitor.LastName}",
                            invitation.HostId,
                            invitation.ScheduledStartTime,
                            invitation.LocationId,
                            cancellationToken
                        );

                        _logger.LogInformation(
                            "No-show notification sent for invitation {InvitationId}",
                            invitation.Id);
                    }
                }
                // Check if this is a delayed case (15-29 minutes late)
                else if (minutesLate >= _delayThresholdMinutes)
                {
                    // Check if we've already sent a delay notification
                    var existingAlerts = await unitOfWork.NotificationAlerts.GetAllAsync(cancellationToken);
                    var hasDelayAlert = existingAlerts?.Any(n =>
                        n.RelatedEntityType == "Invitation" &&
                        n.RelatedEntityId == invitation.Id &&
                        n.Type == NotificationAlertType.VisitorDelayed) ?? false;

                    if (!hasDelayAlert && invitation.Visitor != null)
                    {
                        // Send delayed visitor notification
                        await notificationService.NotifyVisitorDelayedAsync(
                            invitation.Id,
                            invitation.VisitorId,
                            $"{invitation.Visitor.FirstName} {invitation.Visitor.LastName}",
                            invitation.ScheduledStartTime,
                            minutesLate,
                            invitation.LocationId,
                            cancellationToken
                        );

                        _logger.LogInformation(
                            "Delayed visitor notification sent for invitation {InvitationId}, Delay: {DelayMinutes} minutes",
                            invitation.Id,
                            minutesLate);
                    }
                }
            }

            _logger.LogDebug("Visitor monitoring check completed. Processed {Count} invitations", approvedNotCheckedIn.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing visitor monitoring");
            throw;
        }
    }
}
