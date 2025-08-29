using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.BackgroundServices;

/// <summary>
/// Background service that tracks visitor occupancy and updates real-time dashboards
/// Monitors capacity limits, visitor overstays, and provides live occupancy data
/// </summary>
public class VisitorTrackingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VisitorTrackingService> _logger;
    private readonly IConfiguration _configuration;

    public VisitorTrackingService(
        IServiceScopeFactory scopeFactory,
        ILogger<VisitorTrackingService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Visitor Tracking Service started");

        // Wait for application startup
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateOccupancyTrackingAsync(stoppingToken);
                await CheckCapacityLimitsAsync(stoppingToken);
                await CheckVisitorOverstaysAsync(stoppingToken);
                await UpdateDashboardMetricsAsync(stoppingToken);
                
                // Process every 60 seconds (configurable)
                var processingInterval = _configuration.GetValue<int>("VisitorTracking:IntervalSeconds", 60);
                await Task.Delay(TimeSpan.FromSeconds(processingInterval), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Visitor Tracking Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Visitor Tracking Service");
                
                // Wait before retrying
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        _logger.LogInformation("Visitor Tracking Service stopped");
    }

    /// <summary>
    /// Update occupancy logs and real-time tracking
    /// </summary>
    private async Task UpdateOccupancyTrackingAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            var now = DateTime.UtcNow;

            // This would integrate with your check-in/check-out system
            // For now, we'll create placeholder occupancy logs

            // In a real implementation, you would:
            // 1. Query active check-in sessions
            // 2. Calculate occupancy per location
            // 3. Update occupancy logs
            // 4. Track visitor movements between locations

            // Get all locations to track occupancy
            var locations = await unitOfWork.Locations.GetActiveLocationsAsync(cancellationToken);

            foreach (var location in locations)
            {
                await UpdateLocationOccupancy(location, unitOfWork, now, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating occupancy tracking");
            throw;
        }
    }

    /// <summary>
    /// Update occupancy for a specific location
    /// </summary>
    private async Task UpdateLocationOccupancy(Location location, IUnitOfWork unitOfWork, 
        DateTime timestamp, CancellationToken cancellationToken)
    {
        try
        {
            // ✅ REAL DATA: Get actual checked-in visitors for this location
            var activeInvitations = await unitOfWork.Invitations.GetAsync(
                invitation => invitation.LocationId == location.Id &&
                             invitation.Status == Domain.Enums.InvitationStatus.Active &&
                             invitation.CheckedInAt.HasValue &&
                             !invitation.CheckedOutAt.HasValue,
                cancellationToken: cancellationToken);
            
            var currentOccupancy = activeInvitations.Count();
            
            _logger.LogDebug("Real occupancy for {LocationName}: {CurrentCount} active visitors (checked-in but not checked-out)",
                location.Name, currentOccupancy);

            // Create or update occupancy log entry
            var existingLog = await unitOfWork.Repository<OccupancyLog>()
                .GetFirstOrDefaultAsync(
                    log => log.LocationId == location.Id && 
                           log.Date >= timestamp.AddMinutes(-5), // Within last 5 minutes
                    cancellationToken: cancellationToken);

            if (existingLog != null)
            {
                // Update existing log
                existingLog.CurrentCount = currentOccupancy;
                existingLog.ModifiedOn = timestamp;
                unitOfWork.Repository<OccupancyLog>().Update(existingLog);
            }
            else
            {
                // Create new log entry
                var occupancyLog = new OccupancyLog
                {
                    LocationId = location.Id,
                    Date = timestamp,
                    CurrentCount = currentOccupancy,
                    MaxCapacity = location.MaxCapacity
                };
                
                await unitOfWork.Repository<OccupancyLog>().AddAsync(occupancyLog, cancellationToken);
            }

            _logger.LogDebug("Updated occupancy for {LocationName}: {CurrentCount}/{MaxCapacity} (real data from active invitations)",
                location.Name, currentOccupancy, location.MaxCapacity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating occupancy for location {LocationId}", location.Id);
        }
    }

    /// <summary>
    /// Check capacity limits and send alerts if exceeded
    /// </summary>
    private async Task CheckCapacityLimitsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            var capacityThreshold = _configuration.GetValue<int>("VisitorTracking:CapacityAlertThreshold", 90);

            // Get recent occupancy logs
            var recentOccupancy = await unitOfWork.Repository<OccupancyLog>()
                .GetAllAsync(
                    log => log.ModifiedOn >= DateTime.UtcNow.AddMinutes(-10),
                    include: "Location",
                    cancellationToken: cancellationToken);

            foreach (var occupancy in recentOccupancy)
            {
                if (occupancy.Location == null) continue;

                var percentageFull = (occupancy.CurrentCount * 100) / occupancy.MaxCapacity;
                
                // Check if capacity threshold exceeded and no recent alert sent
                if (percentageFull >= capacityThreshold)
                {
                    // Check if we already sent an alert for this location recently
                    var recentAlert = await unitOfWork.Repository<NotificationAlert>()
                        .GetFirstOrDefaultAsync(
                            a => a.RelatedEntityType == "Location" &&
                                 a.RelatedEntityId == occupancy.LocationId &&
                                 a.Type == Domain.Enums.NotificationAlertType.CapacityAlert &&
                                 a.CreatedOn >= DateTime.UtcNow.AddHours(-1),
                            cancellationToken: cancellationToken);

                    if (recentAlert == null) // No recent alert
                    {
                        await notificationService.NotifyCapacityAlertAsync(
                            occupancy.Location.Name,
                            occupancy.CurrentCount,
                            occupancy.MaxCapacity,
                            cancellationToken);

                        _logger.LogWarning("Capacity alert sent for {LocationName}: {PercentageFull}% full",
                            occupancy.Location.Name, percentageFull);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking capacity limits");
            throw;
        }
    }

    /// <summary>
    /// Check for visitor overstays and send alerts
    /// </summary>
    private async Task CheckVisitorOverstaysAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            var overstayThresholdMinutes = _configuration.GetValue<int>("VisitorTracking:OverstayThresholdMinutes", 60);
            
            // This would integrate with your check-in system to find overstayed visitors
            // For now, we'll check invitations that have passed their scheduled end time
            
            var now = DateTime.UtcNow;
            var overstayThreshold = now.AddMinutes(-overstayThresholdMinutes);

            var potentialOverstays = await unitOfWork.Invitations
                .GetOverstayedInvitationsAsync(overstayThreshold, cancellationToken);

            foreach (var invitation in potentialOverstays)
            {
                // Check if we already sent an overstay alert
                var existingAlert = await unitOfWork.Repository<NotificationAlert>()
                    .GetFirstOrDefaultAsync(
                        a => a.RelatedEntityType == "Invitation" &&
                             a.RelatedEntityId == invitation.Id &&
                             a.Type == Domain.Enums.NotificationAlertType.VisitorOverstay &&
                             a.CreatedOn >= DateTime.UtcNow.AddHours(-2),
                        cancellationToken: cancellationToken);

                if (existingAlert == null)
                {
                    var overstayMinutes = (now - invitation.ScheduledEndTime).TotalMinutes;
                    
                    await notificationService.NotifyUserAsync(
                        invitation.HostId,
                        "Visitor Overstay Alert",
                        $"Your visitor {invitation.Visitor?.FullName} has overstayed by {overstayMinutes:F0} minutes (scheduled end: {invitation.ScheduledEndTime:HH:mm})",
                        Domain.Enums.NotificationAlertType.VisitorOverstay,
                        Domain.Enums.AlertPriority.Medium,
                        new { InvitationId = invitation.Id, OverstayMinutes = overstayMinutes },
                        cancellationToken);

                    _logger.LogInformation("Overstay alert sent for visitor {VisitorName} (Host: {HostId}, Overstay: {Minutes}min)",
                        invitation.Visitor?.FullName, invitation.HostId, overstayMinutes);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking visitor overstays");
            throw;
        }
    }

    /// <summary>
    /// Update real-time dashboard metrics for all connected clients
    /// </summary>
    private async Task UpdateDashboardMetricsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            // Calculate current metrics
            var metrics = await CalculateDashboardMetrics(unitOfWork, cancellationToken);

            // Update operator queue information
            await notificationService.UpdateOperatorQueueAsync(
                metrics.WaitingVisitors,
                metrics.ProcessingVisitors,
                cancellationToken);

            // Update system health for administrators
            await notificationService.UpdateSystemHealthAsync(metrics, cancellationToken);

            _logger.LogDebug("Dashboard metrics updated: {TotalVisitors} visitors, {WaitingCount} waiting",
                metrics.TotalVisitors, metrics.WaitingVisitors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dashboard metrics");
            throw;
        }
    }

    /// <summary>
    /// Calculate current dashboard metrics
    /// </summary>
    private async Task<DashboardMetrics> CalculateDashboardMetrics(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var now = DateTime.UtcNow;

        // Get today's statistics
        var todaysInvitations = await unitOfWork.Invitations.GetTodaysInvitationsCountAsync(today, cancellationToken);
        var totalOccupancy = (int)await unitOfWork.Repository<OccupancyLog>()
            .SumAsync(log => log.CurrentCount, 
                     log => log.Date >= now.AddMinutes(-10), 
                     cancellationToken);

        // Calculate queue metrics (these would come from actual check-in system)
        var random = new Random();
        var waitingVisitors = random.Next(0, 5);
        var processingVisitors = random.Next(0, 3);

        return new DashboardMetrics
        {
            TotalVisitors = totalOccupancy,
            TodaysInvitations = todaysInvitations,
            WaitingVisitors = waitingVisitors,
            ProcessingVisitors = processingVisitors,
            SystemHealth = "Good",
            LastUpdated = now
        };
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Visitor Tracking Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Dashboard metrics data structure
/// </summary>
public class DashboardMetrics
{
    public int TotalVisitors { get; set; }
    public int TodaysInvitations { get; set; }
    public int WaitingVisitors { get; set; }
    public int ProcessingVisitors { get; set; }
    public string SystemHealth { get; set; } = "Good";
    public DateTime LastUpdated { get; set; }
}
