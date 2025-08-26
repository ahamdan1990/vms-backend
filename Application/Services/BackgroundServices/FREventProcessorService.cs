using System.Text.Json;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.BackgroundServices;

/// <summary>
/// Background service that processes FR system webhook events and dispatches notifications
/// Simulates receiving FR system webhooks and triggering real-time alerts
/// </summary>
public class FREventProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FREventProcessorService> _logger;
    private readonly IConfiguration _configuration;

    public FREventProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<FREventProcessorService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FR Event Processor Service started");

        // Wait for application startup
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessFREventsAsync(stoppingToken);
                
                // Process every 5 seconds (configurable)
                var processingInterval = _configuration.GetValue<int>("FREventProcessor:IntervalSeconds", 5);
                await Task.Delay(TimeSpan.FromSeconds(processingInterval), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("FR Event Processor Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FR Event Processor Service");
                
                // Wait before retrying
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("FR Event Processor Service stopped");
    }

    /// <summary>
    /// Process queued FR events and dispatch notifications
    /// In production, this would process real FR webhook events from a queue
    /// </summary>
    private async Task ProcessFREventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            // In production, you would:
            // 1. Poll a queue for FR system webhook events
            // 2. Process face detection events
            // 3. Match faces against known visitor profiles
            // 4. Generate appropriate notifications

            // For development/testing, we'll simulate some FR events
            await SimulateFREventsAsync(unitOfWork, notificationService, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FR events");
            throw;
        }
    }

    /// <summary>
    /// Simulate FR events for development/testing
    /// Remove this in production and replace with actual FR webhook processing
    /// </summary>
    private async Task SimulateFREventsAsync(IUnitOfWork unitOfWork, INotificationService notificationService, 
        CancellationToken cancellationToken)
    {
        // Only simulate events in development mode
        if (!_configuration.GetValue<bool>("FREventProcessor:EnableSimulation", false))
            return;

        var random = new Random();
        
        // Randomly generate FR events (very low frequency for demo)
        if (random.Next(1, 1000) <= 2) // 0.2% chance per cycle
        {
            var eventType = random.Next(1, 5);
            
            switch (eventType)
            {
                case 1: // Visitor arrival
                    await SimulateVisitorArrival(notificationService, cancellationToken);
                    break;
                    
                case 2: // VIP arrival
                    await SimulateVipArrival(notificationService, cancellationToken);
                    break;
                    
                case 3: // Unknown face
                    await SimulateUnknownFace(notificationService, cancellationToken);
                    break;
                    
                case 4: // System offline simulation
                    if (random.Next(1, 100) <= 1) // Very rare
                        await SimulateFRSystemOffline(notificationService, cancellationToken);
                    break;
            }
        }
    }

    private async Task SimulateVisitorArrival(INotificationService notificationService, CancellationToken cancellationToken)
    {
        var visitorNames = new[] { "John Doe", "Jane Smith", "Mike Johnson", "Sarah Wilson" };
        var visitorName = visitorNames[new Random().Next(visitorNames.Length)];
        
        // This would normally come from FR system data
        var hostId = 1; // In production, look up actual host from visitor profile
        var visitorId = new Random().Next(1, 100);
        
        await notificationService.NotifyHostOfVisitorArrivalAsync(
            hostId, visitorId, visitorName, DateTime.Now, cancellationToken);
            
        _logger.LogInformation("Simulated visitor arrival: {VisitorName}", visitorName);
    }

    private async Task SimulateVipArrival(INotificationService notificationService, CancellationToken cancellationToken)
    {
        var vipNames = new[] { "CEO John Smith", "Director Mary Johnson", "Minister David Brown" };
        var vipName = vipNames[new Random().Next(vipNames.Length)];
        
        await notificationService.NotifyVipArrivalAsync(vipName, "Main Entrance", cancellationToken);
        
        _logger.LogInformation("Simulated VIP arrival: {VipName}", vipName);
    }

    private async Task SimulateUnknownFace(INotificationService notificationService, CancellationToken cancellationToken)
    {
        var locations = new[] { "Main Entrance", "Side Entrance", "Parking Garage", "Lobby Camera 1" };
        var location = locations[new Random().Next(locations.Length)];
        
        await notificationService.NotifyUnknownFaceDetectionAsync(location, cancellationToken);
        
        _logger.LogInformation("Simulated unknown face detection: {Location}", location);
    }

    private async Task SimulateFRSystemOffline(INotificationService notificationService, CancellationToken cancellationToken)
    {
        await notificationService.NotifyFRSystemOfflineAsync(cancellationToken);
        
        _logger.LogWarning("Simulated FR system offline notification");
    }

    /// <summary>
    /// Process actual FR webhook event (production implementation)
    /// </summary>
    /// <param name="frEventData">FR system event data</param>
    /// <param name="unitOfWork">Unit of work</param>
    /// <param name="notificationService">Notification service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ProcessFRWebhookEvent(string frEventData, IUnitOfWork unitOfWork, 
        INotificationService notificationService, CancellationToken cancellationToken)
    {
        try
        {
            // Parse FR event data
            var frEvent = JsonSerializer.Deserialize<FREventDto>(frEventData);
            if (frEvent == null) return;

            switch (frEvent.EventType?.ToLower())
            {
                case "face_detected":
                    await ProcessFaceDetectedEvent(frEvent, unitOfWork, notificationService, cancellationToken);
                    break;
                    
                case "face_identified":
                    await ProcessFaceIdentifiedEvent(frEvent, unitOfWork, notificationService, cancellationToken);
                    break;
                    
                case "unknown_face":
                    await ProcessUnknownFaceEvent(frEvent, notificationService, cancellationToken);
                    break;
                    
                case "blacklist_match":
                    await ProcessBlacklistMatchEvent(frEvent, notificationService, cancellationToken);
                    break;
                    
                case "system_offline":
                    await notificationService.NotifyFRSystemOfflineAsync(cancellationToken);
                    break;
                    
                default:
                    _logger.LogWarning("Unknown FR event type: {EventType}", frEvent.EventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FR webhook event: {EventData}", frEventData);
            throw;
        }
    }

    private async Task ProcessFaceDetectedEvent(FREventDto frEvent, IUnitOfWork unitOfWork, 
        INotificationService notificationService, CancellationToken cancellationToken)
    {
        // Process face detection and try to identify
        _logger.LogDebug("Processing face detected event at {Location}", frEvent.CameraLocation);
    }

    private async Task ProcessFaceIdentifiedEvent(FREventDto frEvent, IUnitOfWork unitOfWork, 
        INotificationService notificationService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(frEvent.PersonId)) return;

        // Look up visitor by FR person ID
        var visitor = await unitOfWork.Visitors.GetByFRPersonIdAsync(frEvent.PersonId, cancellationToken);
        if (visitor == null) return;

        // Check if visitor is VIP
        if (visitor.IsVip)
        {
            await notificationService.NotifyVipArrivalAsync(
                visitor.FullName, frEvent.CameraLocation ?? "Unknown Location", cancellationToken);
        }
        else
        {
            // Find host for today's appointments
            var todaysInvitation = await unitOfWork.Invitations
                .GetTodaysInvitationForVisitorAsync(visitor.Id, DateTime.Today, cancellationToken);
                
            if (todaysInvitation != null)
            {
                await notificationService.NotifyHostOfVisitorArrivalAsync(
                    todaysInvitation.HostId, visitor.Id, visitor.FullName, 
                    DateTime.Now, cancellationToken);
            }
        }
    }

    private async Task ProcessUnknownFaceEvent(FREventDto frEvent, INotificationService notificationService, 
        CancellationToken cancellationToken)
    {
        await notificationService.NotifyUnknownFaceDetectionAsync(
            frEvent.CameraLocation ?? "Unknown Location", cancellationToken);
    }

    private async Task ProcessBlacklistMatchEvent(FREventDto frEvent, INotificationService notificationService, 
        CancellationToken cancellationToken)
    {
        await notificationService.NotifyBlacklistDetectionAsync(
            frEvent.PersonDescription ?? "Unknown Person", 
            frEvent.CameraLocation ?? "Unknown Location", 
            cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FR Event Processor Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// DTO for FR system webhook events
/// </summary>
public class FREventDto
{
    public string? EventType { get; set; }
    public string? PersonId { get; set; }
    public string? PersonDescription { get; set; }
    public string? CameraLocation { get; set; }
    public string? CameraId { get; set; }
    public DateTime EventTime { get; set; }
    public double? Confidence { get; set; }
    public string? AdditionalData { get; set; }
}
