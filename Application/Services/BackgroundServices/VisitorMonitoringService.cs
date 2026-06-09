using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
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

        try
        {
            await ProcessExpiredInvitationsAsync(unitOfWork, notificationService, now, cancellationToken);
            await ProcessDelayedAndNoShowsAsync(unitOfWork, notificationService, now, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing visitor monitoring");
            throw;
        }
    }

    /// <summary>
    /// Transition Approved invitations past their ScheduledEndTime to Expired status
    /// and notify the host + operators.
    /// </summary>
    private async Task ProcessExpiredInvitationsAsync(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // GetExpiredInvitationsAsync returns Approved invitations whose ScheduledEndTime has passed
        var expiredCandidates = await unitOfWork.Invitations.GetExpiredInvitationsAsync(cancellationToken);

        if (expiredCandidates == null || !expiredCandidates.Any())
            return;

        int expiredCount = 0;
        foreach (var invitation in expiredCandidates)
        {
            try
            {
                invitation.Expire();
                unitOfWork.Invitations.Update(invitation);

                var expireEvent = InvitationEvent.Create(
                    invitation.Id,
                    InvitationEventTypes.Expired,
                    "Invitation expired — scheduled end time passed without check-in",
                    null // system-generated
                );
                await unitOfWork.Repository<InvitationEvent>().AddAsync(expireEvent, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                expiredCount++;

                // Notify host and operators
                var visitorName = invitation.Visitor != null
                    ? $"{invitation.Visitor.FirstName} {invitation.Visitor.LastName}"
                    : "Unknown Visitor";

                await notificationService.NotifyInvitationExpiredAsync(
                    invitation.Id,
                    invitation.HostId,
                    visitorName,
                    invitation.ScheduledEndTime,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring invitation {InvitationId}", invitation.Id);
            }
        }

        if (expiredCount > 0)
            _logger.LogInformation("Expired {Count} invitations", expiredCount);
    }

    /// <summary>
    /// Send delay and no-show notifications for approved invitations that haven't checked in.
    /// </summary>
    private async Task ProcessDelayedAndNoShowsAsync(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Get approved invitations in a recent window that haven't been checked in
        var relevantInvitations = await unitOfWork.Invitations.GetByDateRangeAsync(
            now.AddHours(-2),
            now,
            cancellationToken);

        var approvedNotCheckedIn = relevantInvitations?
            .Where(i => i.Status == InvitationStatus.Approved && !i.IsDeleted)
            .ToList() ?? new List<Invitation>();

        foreach (var invitation in approvedNotCheckedIn)
        {
            if (invitation.ScheduledStartTime > now)
                continue;

            var minutesLate = (int)(now - invitation.ScheduledStartTime).TotalMinutes;

            if (minutesLate >= _noShowThresholdMinutes)
            {
                // Use a targeted DB lookup instead of loading the entire alerts table.
                // No-show is a one-time event per invitation — never re-notify once sent.
                var existingNoShow = await unitOfWork.Repository<NotificationAlert>()
                    .GetFirstOrDefaultAsync(
                        n => n.RelatedEntityType == "Invitation" &&
                             n.RelatedEntityId == invitation.Id &&
                             n.Type == NotificationAlertType.VisitorNoShow,
                        cancellationToken: cancellationToken);

                if (existingNoShow == null && invitation.Visitor != null)
                {
                    await notificationService.NotifyVisitorNoShowAsync(
                        invitation.Id,
                        invitation.VisitorId,
                        $"{invitation.Visitor.FirstName} {invitation.Visitor.LastName}",
                        invitation.HostId,
                        invitation.ScheduledStartTime,
                        invitation.LocationId,
                        cancellationToken);

                    _logger.LogInformation("No-show notification sent for invitation {InvitationId}", invitation.Id);
                }
            }
            else if (minutesLate >= _delayThresholdMinutes)
            {
                // Use a targeted DB lookup instead of loading the entire alerts table.
                // Delayed is a one-time event per invitation — never re-notify once sent.
                var existingDelay = await unitOfWork.Repository<NotificationAlert>()
                    .GetFirstOrDefaultAsync(
                        n => n.RelatedEntityType == "Invitation" &&
                             n.RelatedEntityId == invitation.Id &&
                             n.Type == NotificationAlertType.VisitorDelayed,
                        cancellationToken: cancellationToken);

                if (existingDelay == null && invitation.Visitor != null)
                {
                    await notificationService.NotifyVisitorDelayedAsync(
                        invitation.Id,
                        invitation.VisitorId,
                        $"{invitation.Visitor.FirstName} {invitation.Visitor.LastName}",
                        invitation.ScheduledStartTime,
                        minutesLate,
                        invitation.LocationId,
                        cancellationToken);

                    _logger.LogInformation(
                        "Delayed visitor notification sent for invitation {InvitationId}, Delay: {DelayMinutes} minutes",
                        invitation.Id, minutesLate);
                }
            }
        }

        _logger.LogDebug("Visitor monitoring check completed. Processed {Count} invitations", approvedNotCheckedIn.Count);
    }
}
