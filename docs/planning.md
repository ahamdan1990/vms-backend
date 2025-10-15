# Camera Grabber Service Implementation Roadmap

## Project Overview

**Objective**: Implement a dedicated microservice for camera frame grabbing, facial recognition processing, and GPU-accelerated video analysis that operates independently from the main VMS API.

**Timeline**: 12-16 weeks (3-4 development cycles)  
**Team Size**: 4-6 developers (2 senior, 2-4 mid-level)  
**Critical Dependencies**: RabbitMQ infrastructure, GPU hardware provisioning, facial recognition models

---

## Phase 1: Foundation & Infrastructure (Weeks 1-4)

### 1.1 Project Setup & Infrastructure

#### Task 1.1.1: Development Environment Setup
**Duration**: 3 days  
**Assignee**: Senior Developer  
**Dependencies**: None  

**Implementation Steps**:
```bash
# Create solution structure
mkdir GrabberService
cd GrabberService

# Core service projects
dotnet new webapi -n GrabberService.Api
dotnet new classlib -n GrabberService.Core
dotnet new classlib -n GrabberService.Infrastructure  
dotnet new classlib -n GrabberService.Domain
dotnet new xunit -n GrabberService.Tests
dotnet new xunit -n GrabberService.IntegrationTests

# Create solution file
dotnet new sln
dotnet sln add **/*.csproj
```

**Deliverables**:
- [x] Visual Studio solution with proper project structure
- [x] Docker development environment with GPU support
- [x] Development database configuration
- [x] Basic CI/CD pipeline setup

#### Task 1.1.2: RabbitMQ Infrastructure Setup
**Duration**: 2 days  
**Assignee**: DevOps Engineer + Senior Developer  
**Dependencies**: Task 1.1.1  

**Docker Compose Configuration**:
```yaml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3.12-management
    environment:
      RABBITMQ_DEFAULT_USER: vms_user
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD}
      RABBITMQ_DEFAULT_VHOST: vms
    ports:
      - "5672:5672"
      - "15672:15672"
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    networks:
      - vms-network

volumes:
  rabbitmq_data:
  
networks:
  vms-network:
    driver: bridge
```

**Queue Definitions**:
```csharp
public static class QueueConfiguration
{
    public static void ConfigureQueues(IRabbitMqBusFactoryConfigurator cfg)
    {
        // Camera command queues
        cfg.ReceiveEndpoint("camera-commands", e =>
        {
            e.PrefetchCount = 10;
            e.UseConcurrencyLimit(5);
            e.ConfigureConsumer<CameraCommandConsumer>();
        });
        
        // Facial recognition results
        cfg.ReceiveEndpoint("face-recognition-results", e =>
        {
            e.PrefetchCount = 50;
            e.UseConcurrencyLimit(10);
            e.ConfigureConsumer<FaceRecognitionResultConsumer>();
        });
        
        // Health events
        cfg.ReceiveEndpoint("camera-health-events", e =>
        {
            e.PrefetchCount = 100;
            e.UseConcurrencyLimit(1);
            e.ConfigureConsumer<CameraHealthEventConsumer>();
        });
    }
}
```

**Deliverables**:
- [x] RabbitMQ cluster configuration
- [x] Queue definitions and routing
- [x] Dead letter queue setup
- [x] Management interface configuration

#### Task 1.1.3: Core Domain Models
**Duration**: 2 days  
**Assignee**: Senior Developer  
**Dependencies**: Task 1.1.1  

**Implementation**:
```csharp
// Domain/Entities/CameraSession.cs
public class CameraSession : BaseEntity
{
    public int CameraId { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public CameraSessionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public CameraConfiguration Configuration { get; set; }
    public string? ErrorMessage { get; set; }
    public int FramesProcessed { get; set; }
    public int FacesDetected { get; set; }
    public double AverageProcessingTimeMs { get; set; }
    public GpuResourceAllocation? GpuAllocation { get; set; }
    
    public TimeSpan Duration => EndedAt?.Subtract(StartedAt) ?? DateTime.UtcNow.Subtract(StartedAt);
    
    public void UpdateStatus(CameraSessionStatus newStatus, string? errorMessage = null)
    {
        Status = newStatus;
        ErrorMessage = errorMessage;
        
        if (newStatus == CameraSessionStatus.Ended && !EndedAt.HasValue)
        {
            EndedAt = DateTime.UtcNow;
        }
        
        UpdateModifiedOn();
    }
    
    public void RecordFrameProcessing(double processingTimeMs)
    {
        FramesProcessed++;
        
        // Calculate moving average
        var totalTime = AverageProcessingTimeMs * (FramesProcessed - 1) + processingTimeMs;
        AverageProcessingTimeMs = totalTime / FramesProcessed;
        
        UpdateModifiedOn();
    }
}

// Domain/ValueObjects/GpuResourceAllocation.cs
public class GpuResourceAllocation : ValueObject
{
    public int GpuId { get; init; }
    public long MemoryBytes { get; init; }
    public float ComputeUtilization { get; init; }
    public DateTime AllocatedAt { get; init; }
    public DateTime? ReleasedAt { get; set; }
    
    public bool IsActive => !ReleasedAt.HasValue;
    public TimeSpan AllocationDuration => ReleasedAt?.Subtract(AllocatedAt) ?? DateTime.UtcNow.Subtract(AllocatedAt);
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return GpuId;
        yield return MemoryBytes;
        yield return AllocatedAt;
    }
}

// Domain/Events/CameraSessionStartedEvent.cs
public record CameraSessionStartedEvent(
    int CameraId,
    string SessionId,
    string InstanceId,
    CameraConfiguration Configuration,
    DateTime StartedAt
) : IDomainEvent;
```

**Deliverables**:
- [x] Core domain entities with business logic
- [x] Value objects for complex types
- [x] Domain events for cross-boundary communication
- [x] Repository interfaces

### 1.2 Message Queue Integration

#### Task 1.2.1: MassTransit Configuration
**Duration**: 3 days  
**Assignee**: Mid-level Developer  
**Dependencies**: Task 1.1.2  

**Implementation**:
```csharp
// Infrastructure/Messaging/MassTransitConfiguration.cs
public static class MassTransitConfiguration
{
    public static IServiceCollection AddMassTransitWithRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // Configure consumers
            x.AddConsumer<CameraCommandConsumer>();
            x.AddConsumer<FaceRecognitionResultConsumer>();
            x.AddConsumer<CameraHealthEventConsumer>();
            
            x.UsingRabbitMq((context, cfg) =>
            {
                var connectionString = configuration.GetConnectionString("RabbitMQ");
                cfg.Host(connectionString);
                
                // Configure retry policies
                cfg.UseMessageRetry(r => r.Exponential(5, 
                    TimeSpan.FromSeconds(1), 
                    TimeSpan.FromMinutes(5), 
                    TimeSpan.FromSeconds(5)));
                
                // Configure error handling
                cfg.UseInMemoryOutbox();
                cfg.ConfigureEndpoints(context);
                
                // Performance optimizations
                cfg.PrefetchCount = 100;
                cfg.ConcurrentMessageLimit = 50;
            });
        });
        
        return services;
    }
}

// Application/Consumers/CameraCommandConsumer.cs
public class CameraCommandConsumer : IConsumer<CameraCommand>
{
    private readonly ICameraSessionManager _sessionManager;
    private readonly IFrameCaptureEngine _captureEngine;
    private readonly ILogger<CameraCommandConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    
    public async Task Consume(ConsumeContext<CameraCommand> context)
    {
        var command = context.Message;
        var requestId = context.RequestId?.ToString() ?? Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Processing camera command {CommandType} for camera {CameraId}", 
                command.CommandType, command.CameraId);
            
            var result = command.CommandType switch
            {
                CameraCommandType.Start => await HandleStartCommand(command, requestId),
                CameraCommandType.Stop => await HandleStopCommand(command, requestId),
                CameraCommandType.Restart => await HandleRestartCommand(command, requestId),
                CameraCommandType.UpdateConfiguration => await HandleUpdateConfiguration(command, requestId),
                _ => throw new InvalidOperationException($"Unknown command type: {command.CommandType}")
            };
            
            // Publish result
            await _publishEndpoint.Publish(new CameraCommandResult
            {
                RequestId = requestId,
                CameraId = command.CameraId,
                CommandType = command.CommandType,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                ProcessedAt = DateTime.UtcNow
            });
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing camera command {CommandType} for camera {CameraId}", 
                command.CommandType, command.CameraId);
            
            await _publishEndpoint.Publish(new CameraCommandResult
            {
                RequestId = requestId,
                CameraId = command.CameraId,
                CommandType = command.CommandType,
                Success = false,
                ErrorMessage = ex.Message,
                ProcessedAt = DateTime.UtcNow
            });
        }
    }
    
    private async Task<CameraOperationResult> HandleStartCommand(CameraCommand command, string requestId)
    {
        var session = await _sessionManager.CreateSessionAsync(command.CameraId, requestId, command.Configuration);
        
        if (session == null)
        {
            return CameraOperationResult.Failure("Failed to create camera session");
        }
        
        var started = await _captureEngine.StartCaptureAsync(session);
        return started 
            ? CameraOperationResult.Success($"Camera session {session.Id} started")
            : CameraOperationResult.Failure("Failed to start camera capture");
    }
}
```

**Deliverables**:
- [x] MassTransit configuration with RabbitMQ
- [x] Message consumers for all command types
- [x] Retry policies and error handling
- [x] Performance optimization settings

#### Task 1.2.2: VMS API Integration Layer
**Duration**: 4 days  
**Assignee**: Senior Developer  
**Dependencies**: Task 1.2.1  

**VMS API Service Implementation**:
```csharp
// VMS API - Application/Services/Cameras/GrabberClientService.cs
public class GrabberClientService : IGrabberClientService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IRequestClient<CameraCommand> _commandClient;
    private readonly ILogger<GrabberClientService> _logger;
    private readonly IConfiguration _configuration;
    
    public GrabberClientService(
        IPublishEndpoint publishEndpoint,
        IRequestClient<CameraCommand> commandClient,
        ILogger<GrabberClientService> logger,
        IConfiguration configuration)
    {
        _publishEndpoint = publishEndpoint;
        _commandClient = commandClient;
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task<bool> StartCameraAsync(int cameraId, CameraStartOptions options)
    {
        try
        {
            var command = new CameraCommand
            {
                CameraId = cameraId,
                CommandType = CameraCommandType.Start,
                Configuration = options.Configuration,
                EnableFacialRecognition = options.EnableFacialRecognition,
                Priority = options.Priority,
                RequestedBy = options.UserId,
                RequestedAt = DateTime.UtcNow
            };
            
            var timeout = TimeSpan.FromSeconds(_configuration.GetValue<int>("Grabber:CommandTimeoutSeconds", 30));
            var response = await _commandClient.GetResponse<CameraCommandResult>(command, timeout);
            
            if (response.Message.Success)
            {
                _logger.LogInformation("Successfully started camera {CameraId}", cameraId);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to start camera {CameraId}: {Error}", 
                    cameraId, response.Message.ErrorMessage);
                return false;
            }
        }
        catch (RequestTimeoutException)
        {
            _logger.LogError("Timeout starting camera {CameraId}", cameraId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting camera {CameraId}", cameraId);
            return false;
        }
    }
    
    public async Task<GrabberServiceMetrics> GetServiceMetricsAsync()
    {
        try
        {
            var metricsClient = _serviceProvider.GetRequiredService<IRequestClient<GetMetricsQuery>>();
            var response = await metricsClient.GetResponse<GrabberServiceMetrics>(
                new GetMetricsQuery(), 
                TimeSpan.FromSeconds(10));
            
            return response.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get grabber service metrics");
            return new GrabberServiceMetrics
            {
                IsAvailable = false,
                ErrorMessage = ex.Message,
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}

// VMS API - Enhanced CamerasController
public partial class CamerasController
{
    private readonly IGrabberClientService _grabberClient;
    
    [HttpPost("{id:int}/start-stream")]
    public async Task<IActionResult> StartStream(int id)
    {
        try
        {
            var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
            if (camera == null) return NotFoundResponse("Camera", id);
            
            // Update database status to "Starting"
            await _mediator.Send(new UpdateCameraStatusCommand 
            { 
                Id = id, 
                Status = CameraStatus.Connecting,
                ModifiedBy = GetCurrentUserId()
            });
            
            var options = new CameraStartOptions
            {
                EnableFacialRecognition = camera.EnableFacialRecognition,
                Configuration = camera.GetConfiguration(),
                Priority = camera.Priority,
                UserId = GetCurrentUserId()
            };
            
            var result = await _grabberClient.StartCameraAsync(id, options);
            
            if (!result)
            {
                // Revert status on failure
                await _mediator.Send(new UpdateCameraStatusCommand 
                { 
                    Id = id, 
                    Status = CameraStatus.Error,
                    ErrorMessage = "Failed to start camera stream",
                    ModifiedBy = GetCurrentUserId()
                });
            }
            
            return SuccessResponse(new 
            { 
                Success = result, 
                StartedAt = DateTime.UtcNow,
                Message = result ? "Camera stream start command sent successfully" : "Failed to start camera stream"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting stream for camera {CameraId}", id);
            return ServerErrorResponse("An error occurred while starting camera stream");
        }
    }
}
```

**Deliverables**:
- [x] IGrabberClientService implementation
- [x] Enhanced CamerasController with async operations
- [x] Request/response pattern for reliable communication
- [x] Error handling and status management

---

## Phase 2: Core Camera Engine (Weeks 5-8)

### 2.1 Frame Capture Infrastructure

#### Task 2.1.1: Camera Handler Implementations
**Duration**: 5 days  
**Assignee**: Senior Developer + Mid-level Developer  
**Dependencies**: Phase 1 completion  

**RTSP Handler Implementation**:
```csharp
// Core/Cameras/Handlers/RtspCameraHandler.cs
public class RtspCameraHandler : ICameraHandler
{
    private readonly ILogger<RtspCameraHandler> _logger;
    private readonly IFrameBuffer _frameBuffer;
    private VideoCapture? _capture;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _captureTask;
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    
    public async Task<bool> StartAsync(CameraConfiguration configuration)
    {
        await _stateLock.WaitAsync();
        try
        {
            if (IsRunning)
            {
                _logger.LogWarning("Camera handler already running");
                return true;
            }
            
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Initialize OpenCV VideoCapture with optimized settings
            _capture = new VideoCapture();
            
            // Set buffer size to minimize latency
            _capture.Set(VideoCaptureProperties.BufferSize, 1);
            
            // Configure codec preferences for performance
            _capture.Set(VideoCaptureProperties.FourCC, VideoWriter.FourCC('H', '2', '6', '4'));
            
            // Set resolution if specified
            if (configuration.ResolutionWidth.HasValue && configuration.ResolutionHeight.HasValue)
            {
                _capture.Set(VideoCaptureProperties.FrameWidth, configuration.ResolutionWidth.Value);
                _capture.Set(VideoCaptureProperties.FrameHeight, configuration.ResolutionHeight.Value);
            }
            
            // Set framerate if specified
            if (configuration.FrameRate.HasValue)
            {
                _capture.Set(VideoCaptureProperties.Fps, configuration.FrameRate.Value);
            }
            
            var connectionString = BuildConnectionString(configuration);
            var opened = _capture.Open(connectionString);
            
            if (!opened)
            {
                _logger.LogError("Failed to open RTSP stream: {ConnectionString}", 
                    GetSafeConnectionString(connectionString));
                return false;
            }
            
            // Start capture loop
            _captureTask = Task.Run(() => CaptureLoop(_cancellationTokenSource.Token));
            
            _logger.LogInformation("RTSP camera handler started successfully");
            return true;
        }
        finally
        {
            _stateLock.Release();
        }
    }
    
    private async Task CaptureLoop(CancellationToken cancellationToken)
    {
        var frameCounter = 0;
        var lastFrameTime = DateTime.UtcNow;
        var frame = new Mat();
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && _capture?.IsOpened == true)
            {
                var frameStart = DateTime.UtcNow;
                
                // Capture frame with timeout
                var captured = _capture.Read(frame);
                
                if (!captured || frame.Empty())
                {
                    _logger.LogWarning("Failed to capture frame, attempting recovery");
                    await AttemptRecovery(cancellationToken);
                    continue;
                }
                
                frameCounter++;
                var frameCaptureTime = DateTime.UtcNow - frameStart;
                
                // Create frame data with metadata
                var frameData = new CapturedFrame
                {
                    Data = frame.ToBytes(),
                    CaptureTime = frameStart,
                    FrameNumber = frameCounter,
                    Width = frame.Width,
                    Height = frame.Height,
                    Channels = frame.Channels(),
                    ProcessingTimeMs = frameCaptureTime.TotalMilliseconds
                };
                
                // Add to processing queue
                await _frameBuffer.EnqueueAsync(frameData, cancellationToken);
                
                // Log performance metrics periodically
                if (frameCounter % 100 == 0)
                {
                    var elapsed = DateTime.UtcNow - lastFrameTime;
                    var fps = 100.0 / elapsed.TotalSeconds;
                    _logger.LogDebug("Captured 100 frames, current FPS: {FPS:F1}", fps);
                    lastFrameTime = DateTime.UtcNow;
                }
                
                // Adaptive frame rate control
                if (frameCaptureTime.TotalMilliseconds > 50) // >50ms capture time
                {
                    await Task.Delay(10, cancellationToken); // Brief pause to prevent overload
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Frame capture cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Frame capture loop error");
            await NotifyError("Frame capture failed", ex);
        }
        finally
        {
            frame?.Dispose();
        }
    }
    
    private async Task AttemptRecovery(CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 1000;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Recovery attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                
                // Reinitialize capture
                _capture?.Release();
                _capture = new VideoCapture();
                
                var reopened = _capture.Open(_currentConnectionString);
                if (reopened)
                {
                    _logger.LogInformation("Recovery successful on attempt {Attempt}", attempt);
                    await NotifyRecovery(attempt);
                    return;
                }
                
                if (attempt < maxRetries)
                {
                    await Task.Delay(retryDelayMs * attempt, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Recovery attempt {Attempt} failed", attempt);
            }
        }
        
        _logger.LogError("Recovery failed after {MaxRetries} attempts", maxRetries);
        await NotifyError("Recovery failed", null);
    }
}

// Core/Cameras/FrameBuffer.cs
public class FrameBuffer : IFrameBuffer
{
    private readonly Channel<CapturedFrame> _channel;
    private readonly ILogger<FrameBuffer> _logger;
    private readonly FrameBufferMetrics _metrics = new();
    
    public FrameBuffer(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest, // Drop old frames under pressure
            SingleReader = false,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        };
        
        _channel = Channel.CreateBounded<CapturedFrame>(options);
    }
    
    public async Task EnqueueAsync(CapturedFrame frame, CancellationToken cancellationToken = default)
    {
        var enqueued = await _channel.Writer.WaitToWriteAsync(cancellationToken);
        
        if (enqueued && _channel.Writer.TryWrite(frame))
        {
            _metrics.FramesEnqueued++;
            _metrics.CurrentQueueSize = _channel.Reader.Count;
        }
        else
        {
            _metrics.FramesDropped++;
            _logger.LogWarning("Failed to enqueue frame, buffer may be full");
        }
    }
    
    public async Task<CapturedFrame?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var frame = await _channel.Reader.ReadAsync(cancellationToken);
            _metrics.FramesDequeued++;
            _metrics.CurrentQueueSize = _channel.Reader.Count;
            return frame;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
    
    public FrameBufferMetrics GetMetrics() => _metrics.Clone();
}
```

**Deliverables**:
- [x] RTSP camera handler with OpenCV integration
- [x] USB camera handler for local devices
- [x] IP camera handler with HTTP/HTTPS support
- [x] ONVIF camera handler with device discovery
- [x] Frame buffer with backpressure handling
- [x] Performance monitoring and metrics

#### Task 2.1.2: Camera Session Manager
**Duration**: 3 days  
**Assignee**: Mid-level Developer  
**Dependencies**: Task 2.1.1  

**Implementation**:
```csharp
// Core/Cameras/CameraSessionManager.cs
public class CameraSessionManager : ICameraSessionManager
{
    private readonly Dictionary<int, CameraSession> _activeSessions = new();
    private readonly Dictionary<int, ICameraHandler> _handlers = new();
    private readonly IGpuResourceManager _gpuManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CameraSessionManager> _logger;
    private readonly ReaderWriterLockSlim _sessionLock = new();
    
    public async Task<CameraSession?> CreateSessionAsync(int cameraId, string requestId, 
        CameraConfiguration configuration)
    {
        _sessionLock.EnterWriteLock();
        try
        {
            // Check if session already exists
            if (_activeSessions.ContainsKey(cameraId))
            {
                _logger.LogWarning("Camera session already exists for camera {CameraId}", cameraId);
                return _activeSessions[cameraId];
            }
            
            // Allocate GPU resources
            var gpuAllocation = await _gpuManager.AllocateAsync(new GpuResourceRequest
            {
                CameraId = cameraId,
                EstimatedMemoryMB = EstimateMemoryRequirement(configuration),
                RequiredComputeCapability = 6.0f, // Minimum for facial recognition
                Priority = configuration.Priority ?? 5
            });
            
            if (gpuAllocation == null)
            {
                _logger.LogError("Failed to allocate GPU resources for camera {CameraId}", cameraId);
                return null;
            }
            
            // Create camera handler based on type
            var handler = CreateCameraHandler(configuration.CameraType);
            if (handler == null)
            {
                await _gpuManager.ReleaseAsync(gpuAllocation);
                _logger.LogError("Failed to create camera handler for type {CameraType}", 
                    configuration.CameraType);
                return null;
            }
            
            // Create session
            var session = new CameraSession
            {
                Id = Guid.NewGuid().ToString(),
                CameraId = cameraId,
                RequestId = requestId,
                Configuration = configuration,
                Status = CameraSessionStatus.Initializing,
                StartedAt = DateTime.UtcNow,
                GpuAllocation = gpuAllocation,
                InstanceId = Environment.MachineName
            };
            
            // Store session and handler
            _activeSessions[cameraId] = session;
            _handlers[cameraId] = handler;
            
            _logger.LogInformation("Created camera session {SessionId} for camera {CameraId}", 
                session.Id, cameraId);
            
            return session;
        }
        finally
        {
            _sessionLock.ExitWriteLock();
        }
    }
    
    public async Task<bool> StartSessionAsync(int cameraId)
    {
        _sessionLock.EnterReadLock();
        CameraSession? session = null;
        ICameraHandler? handler = null;
        
        try
        {
            if (!_activeSessions.TryGetValue(cameraId, out session) ||
                !_handlers.TryGetValue(cameraId, out handler))
            {
                _logger.LogWarning("No session or handler found for camera {CameraId}", cameraId);
                return false;
            }
        }
        finally
        {
            _sessionLock.ExitReadLock();
        }
        
        try
        {
            session.UpdateStatus(CameraSessionStatus.Starting);
            
            // Start camera handler
            var started = await handler.StartAsync(session.Configuration);
            
            if (started)
            {
                session.UpdateStatus(CameraSessionStatus.Active);
                
                // Start facial recognition if enabled
                if (session.Configuration.EnableFacialRecognition)
                {
                    await StartFacialRecognitionAsync(session);
                }
                
                _logger.LogInformation("Camera session {SessionId} started successfully", session.Id);
                return true;
            }
            else
            {
                session.UpdateStatus(CameraSessionStatus.Error, "Failed to start camera handler");
                _logger.LogError("Failed to start camera handler for session {SessionId}", session.Id);
                return false;
            }
        }
        catch (Exception ex)
        {
            session?.UpdateStatus(CameraSessionStatus.Error, ex.Message);
            _logger.LogError(ex, "Error starting camera session {SessionId}", session?.Id);
            return false;
        }
    }
    
    private async Task StartFacialRecognitionAsync(CameraSession session)
    {
        var frEngine = _serviceProvider.GetRequiredService<IFacialRecognitionEngine>();
        
        await frEngine.StartProcessingAsync(new FacialRecognitionOptions
        {
            CameraId = session.CameraId,
            SessionId = session.Id,
            GpuAllocation = session.GpuAllocation,
            ConfidenceThreshold = session.Configuration.FacialRecognitionThreshold ?? 0.8f,
            MaxFacesPerFrame = 10,
            EnableLivenessDetection = true
        });
        
        _logger.LogInformation("Started facial recognition for camera {CameraId}", session.CameraId);
    }
    
    private int EstimateMemoryRequirement(CameraConfiguration configuration)
    {
        // Base memory for frame buffer (1080p RGB)
        var baseMemoryMB = 8;
        
        // Additional memory for higher resolutions
        if (configuration.ResolutionWidth > 1920 || configuration.ResolutionHeight > 1080)
        {
            baseMemoryMB *= 2;
        }
        
        // Additional memory for facial recognition
        if (configuration.EnableFacialRecognition)
        {
            baseMemoryMB += 200; // Face detection and recognition models
        }
        
        // Buffer for batch processing
        baseMemoryMB += 50;
        
        return baseMemoryMB;
    }
}
```

**Deliverables**:
- [x] Session lifecycle management
- [x] GPU resource allocation integration
- [x] Camera handler factory pattern
- [x] Concurrent session handling
- [x] Error recovery mechanisms

### 2.2 GPU Resource Management

#### Task 2.2.1: GPU Memory Pool Implementation
**Duration**: 4 days  
**Assignee**: Senior Developer  
**Dependencies**: Task 2.1.2  

**Implementation**:
```csharp
// Core/GPU/GpuResourceManager.cs
public class GpuResourceManager : IGpuResourceManager, IDisposable
{
    private readonly Dictionary<int, GpuDevice> _availableDevices = new();
    private readonly Dictionary<int, GpuAllocation> _activeAllocations = new();
    private readonly SemaphoreSlim _allocationLock = new(1, 1);
    private readonly ILogger<GpuResourceManager> _logger;
    private readonly GpuMetricsCollector _metricsCollector;
    private readonly Timer _cleanupTimer;
    private volatile bool _disposed;
    
    public GpuResourceManager(ILogger<GpuResourceManager> logger)
    {
        _logger = logger;
        _metricsCollector = new GpuMetricsCollector();
        
        InitializeGpuDevices();
        
        // Cleanup timer every 30 seconds
        _cleanupTimer = new Timer(PerformCleanup, null, 
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }
    
    public async Task<GpuAllocation?> AllocateAsync(GpuResourceRequest request)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(GpuResourceManager));
        
        await _allocationLock.WaitAsync();
        try
        {
            // Find best GPU device for the request
            var bestDevice = FindBestDevice(request);
            if (bestDevice == null)
            {
                _logger.LogWarning("No suitable GPU device available for request: {Request}", request);
                return null;
            }
            
            // Check memory availability
            var availableMemory = await GetAvailableMemoryAsync(bestDevice.Id);
            if (availableMemory < request.EstimatedMemoryMB)
            {
                _logger.LogWarning("Insufficient GPU memory. Required: {Required}MB, Available: {Available}MB", 
                    request.EstimatedMemoryMB, availableMemory);
                
                // Attempt garbage collection
                await TriggerGarbageCollectionAsync(bestDevice.Id);
                
                // Check again after cleanup
                availableMemory = await GetAvailableMemoryAsync(bestDevice.Id);
                if (availableMemory < request.EstimatedMemoryMB)
                {
                    return null;
                }
            }
            
            // Create allocation
            var allocation = new GpuAllocation
            {
                Id = Guid.NewGuid().ToString(),
                CameraId = request.CameraId,
                GpuDeviceId = bestDevice.Id,
                MemoryMB = request.EstimatedMemoryMB,
                AllocatedAt = DateTime.UtcNow,
                Priority = request.Priority,
                DeviceContext = await CreateDeviceContextAsync(bestDevice.Id),
                MemoryHandles = await AllocateGpuMemoryAsync(bestDevice.Id, request.EstimatedMemoryMB)
            };
            
            _activeAllocations[request.CameraId] = allocation;
            bestDevice.AllocatedMemoryMB += request.EstimatedMemoryMB;
            bestDevice.ActiveAllocations++;
            
            _logger.LogInformation("Allocated {MemoryMB}MB GPU memory on device {DeviceId} for camera {CameraId}", 
                allocation.MemoryMB, allocation.GpuDeviceId, allocation.CameraId);
            
            return allocation;
        }
        finally
        {
            _allocationLock.Release();
        }
    }
    
    private GpuDevice? FindBestDevice(GpuResourceRequest request)
    {
        var availableDevices = _availableDevices.Values
            .Where(d => d.ComputeCapability >= request.RequiredComputeCapability)
            .Where(d => d.IsHealthy)
            .OrderBy(d => d.CurrentUtilization)
            .ThenBy(d => d.AllocatedMemoryMB)
            .ToList();
        
        if (!availableDevices.Any())
        {
            _logger.LogWarning("No healthy GPU devices available with required compute capability {Capability}", 
                request.RequiredComputeCapability);
            return null;
        }
        
        // Prefer device with lowest utilization and sufficient memory
        foreach (var device in availableDevices)
        {
            var estimatedUsage = (device.AllocatedMemoryMB + request.EstimatedMemoryMB) / (double)device.TotalMemoryMB;
            if (estimatedUsage < 0.9) // Don't exceed 90% memory usage
            {
                return device;
            }
        }
        
        return null;
    }
    
    private async Task<CudaDeviceContext> CreateDeviceContextAsync(int deviceId)
    {
        return await Task.Run(() =>
        {
            // Set CUDA device context
            var result = CudaApi.cudaSetDevice(deviceId);
            if (result != CudaError.Success)
            {
                throw new InvalidOperationException($"Failed to set CUDA device {deviceId}: {result}");
            }
            
            // Create CUDA context
            var context = new CudaDeviceContext(deviceId);
            context.Initialize();
            
            return context;
        });
    }
    
    private async Task<List<GpuMemoryHandle>> AllocateGpuMemoryAsync(int deviceId, int memoryMB)
    {
        var handles = new List<GpuMemoryHandle>();
        var bytesToAllocate = memoryMB * 1024 * 1024;
        
        try
        {
            // Allocate in chunks for better memory management
            var chunkSize = Math.Min(bytesToAllocate, 100 * 1024 * 1024); // 100MB chunks
            var remaining = bytesToAllocate;
            
            while (remaining > 0)
            {
                var currentChunk = Math.Min(remaining, chunkSize);
                
                var result = CudaApi.cudaMalloc(out IntPtr devPtr, (uint)currentChunk);
                if (result != CudaError.Success)
                {
                    // Cleanup already allocated memory
                    foreach (var handle in handles)
                    {
                        CudaApi.cudaFree(handle.DevicePointer);
                    }
                    
                    throw new OutOfMemoryException($"Failed to allocate {currentChunk} bytes on GPU {deviceId}: {result}");
                }
                
                handles.Add(new GpuMemoryHandle
                {
                    DevicePointer = devPtr,
                    SizeBytes = currentChunk,
                    AllocatedAt = DateTime.UtcNow
                });
                
                remaining -= currentChunk;
            }
            
            _logger.LogDebug("Allocated {Count} memory chunks totaling {MemoryMB}MB on GPU {DeviceId}", 
                handles.Count, memoryMB, deviceId);
            
            return handles;
        }
        catch
        {
            // Ensure cleanup on failure
            foreach (var handle in handles)
            {
                CudaApi.cudaFree(handle.DevicePointer);
            }
            throw;
        }
    }
    
    public async Task ReleaseAsync(GpuAllocation allocation)
    {
        if (allocation == null) return;
        
        await _allocationLock.WaitAsync();
        try
        {
            // Release GPU memory
            foreach (var handle in allocation.MemoryHandles)
            {
                var result = CudaApi.cudaFree(handle.DevicePointer);
                if (result != CudaError.Success)
                {
                    _logger.LogWarning("Failed to free GPU memory: {Error}", result);
                }
            }
            
            // Update device statistics
            if (_availableDevices.TryGetValue(allocation.GpuDeviceId, out var device))
            {
                device.AllocatedMemoryMB -= allocation.MemoryMB;
                device.ActiveAllocations--;
            }
            
            // Remove from active allocations
            _activeAllocations.Remove(allocation.CameraId);
            
            // Dispose device context
            allocation.DeviceContext?.Dispose();
            
            allocation.ReleasedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Released {MemoryMB}MB GPU memory for camera {CameraId}", 
                allocation.MemoryMB, allocation.CameraId);
        }
        finally
        {
            _allocationLock.Release();
        }
    }
    
    private void InitializeGpuDevices()
    {
        var deviceCount = CudaApi.GetDeviceCount();
        
        for (int i = 0; i < deviceCount; i++)
        {
            try
            {
                var properties = CudaApi.GetDeviceProperties(i);
                var device = new GpuDevice
                {
                    Id = i,
                    Name = properties.Name,
                    TotalMemoryMB = (int)(properties.TotalGlobalMemory / (1024 * 1024)),
                    ComputeCapability = properties.Major + (properties.Minor / 10.0f),
                    MaxThreadsPerBlock = properties.MaxThreadsPerBlock,
                    IsHealthy = true,
                    LastHealthCheck = DateTime.UtcNow
                };
                
                _availableDevices[i] = device;
                
                _logger.LogInformation("Initialized GPU device {Id}: {Name} ({MemoryMB}MB, CC {ComputeCapability})", 
                    i, device.Name, device.TotalMemoryMB, device.ComputeCapability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize GPU device {Id}", i);
            }
        }
        
        if (!_availableDevices.Any())
        {
            _logger.LogWarning("No GPU devices available for processing");
        }
    }
}

// Core/GPU/GpuMetricsCollector.cs
public class GpuMetricsCollector : BackgroundService
{
    private readonly ILogger<GpuMetricsCollector> _logger;
    private readonly Dictionary<int, GpuMetrics> _deviceMetrics = new();
    private readonly Timer _collectionTimer;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectMetrics();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting GPU metrics");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
    
    private async Task CollectMetrics()
    {
        var deviceCount = CudaApi.GetDeviceCount();
        
        for (int i = 0; i < deviceCount; i++)
        {
            try
            {
                var metrics = await CollectDeviceMetrics(i);
                _deviceMetrics[i] = metrics;
                
                // Log warnings for high utilization
                if (metrics.MemoryUtilization > 0.9)
                {
                    _logger.LogWarning("GPU {DeviceId} memory utilization high: {Utilization:P1}", 
                        i, metrics.MemoryUtilization);
                }
                
                if (metrics.GpuUtilization > 0.95)
                {
                    _logger.LogWarning("GPU {DeviceId} compute utilization high: {Utilization:P1}", 
                        i, metrics.GpuUtilization);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect metrics for GPU {DeviceId}", i);
            }
        }
    }
    
    private async Task<GpuMetrics> CollectDeviceMetrics(int deviceId)
    {
        return await Task.Run(() =>
        {
            // Get memory info
            CudaApi.cudaMemGetInfo(out ulong freeBytes, out ulong totalBytes);
            var usedBytes = totalBytes - freeBytes;
            
            // Get GPU utilization (requires NVML)
            var gpuUtilization = NvmlApi.GetUtilizationRates(deviceId);
            var temperature = NvmlApi.GetTemperature(deviceId);
            var powerUsage = NvmlApi.GetPowerUsage(deviceId);
            
            return new GpuMetrics
            {
                DeviceId = deviceId,
                MemoryUtilization = usedBytes / (double)totalBytes,
                GpuUtilization = gpuUtilization.Gpu / 100.0,
                MemoryUtilization = gpuUtilization.Memory / 100.0,
                Temperature = temperature,
                PowerUsageWatts = powerUsage,
                Timestamp = DateTime.UtcNow
            };
        });
    }
}
```

**Deliverables**:
- [x] GPU device discovery and initialization
- [x] Memory allocation with chunking strategy
- [x] Resource cleanup and garbage collection
- [x] Performance metrics collection
- [x] Multi-GPU support with load balancing

#### Task 2.2.2: Batch Processing Engine
**Duration**: 4 days  
**Assignee**: Mid-level Developer  
**Dependencies**: Task 2.2.1  

**Implementation**:
```csharp
// Core/Processing/BatchProcessingEngine.cs
public class BatchProcessingEngine : BackgroundService
{
    private readonly Channel<FrameProcessingRequest> _requestChannel;
    private readonly IGpuResourceManager _gpuManager;
    private readonly IFacialRecognitionEngine _recognitionEngine;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<BatchProcessingEngine> _logger;
    private readonly BatchProcessingOptions _options;
    
    public BatchProcessingEngine(
        IGpuResourceManager gpuManager,
        IFacialRecognitionEngine recognitionEngine,
        IPublishEndpoint publisher,
        ILogger<BatchProcessingEngine> logger,
        IOptions<BatchProcessingOptions> options)
    {
        _gpuManager = gpuManager;
        _recognitionEngine = recognitionEngine;
        _publisher = publisher;
        _logger = logger;
        _options = options.Value;
        
        var channelOptions = new BoundedChannelOptions(_options.MaxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        };
        
        _requestChannel = Channel.CreateBounded<FrameProcessingRequest>(channelOptions);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatch(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch processing error");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }, stoppingToken);
    }
    
    private async Task ProcessBatch(CancellationToken cancellationToken)
    {
        var batch = new List<FrameProcessingRequest>(_options.MaxBatchSize);
        var batchTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        batchTimeout.CancelAfter(TimeSpan.FromMilliseconds(_options.MaxWaitTimeMs));
        
        try
        {
            // Collect batch with timeout
            var batchStartTime = DateTime.UtcNow;
            
            while (batch.Count < _options.MaxBatchSize &&
                   (DateTime.UtcNow - batchStartTime).TotalMilliseconds < _options.MaxWaitTimeMs)
            {
                try
                {
                    var request = await _requestChannel.Reader.ReadAsync(batchTimeout.Token);
                    batch.Add(request);
                }
                catch (OperationCanceledException) when (batchTimeout.Token.IsCancellationRequested)
                {
                    break; // Timeout reached, process what we have
                }
            }
            
            if (batch.Count == 0) return;
            
            _logger.LogDebug("Processing batch of {Count} frames", batch.Count);
            
            // Group by camera for efficient processing
            var cameraGroups = batch.GroupBy(r => r.CameraId).ToList();
            var processingTasks = new List<Task>();
            
            foreach (var group in cameraGroups)
            {
                var cameraFrames = group.ToList();
                var task = ProcessCameraFrames(cameraFrames, cancellationToken);
                processingTasks.Add(task);
            }
            
            // Wait for all camera processing to complete
            await Task.WhenAll(processingTasks);
            
            var processingTime = DateTime.UtcNow - batchStartTime;
            _logger.LogDebug("Completed batch processing in {ProcessingTime}ms", 
                processingTime.TotalMilliseconds);
        }
        finally
        {
            batchTimeout?.Dispose();
        }
    }
    
    private async Task ProcessCameraFrames(List<FrameProcessingRequest> frames, 
        CancellationToken cancellationToken)
    {
        var cameraId = frames.First().CameraId;
        GpuAllocation? gpuAllocation = null;
        
        try
        {
            // Get or create GPU allocation for this camera
            gpuAllocation = await _gpuManager.GetAllocationAsync(cameraId);
            if (gpuAllocation == null)
            {
                _logger.LogWarning("No GPU allocation available for camera {CameraId}", cameraId);
                await HandleProcessingFailure(frames, "No GPU resources available");
                return;
            }
            
            // Upload frames to GPU in batch
            using var gpuFrameBatch = await UploadFramesToGpu(frames, gpuAllocation);
            
            // Process facial recognition for all frames
            var recognitionResults = await _recognitionEngine.ProcessFrameBatchAsync(
                gpuFrameBatch, cancellationToken);
            
            // Publish results
            var publishTasks = recognitionResults.Select(result => 
                PublishResult(result, cancellationToken));
            
            await Task.WhenAll(publishTasks);
            
            // Update session metrics
            await UpdateSessionMetrics(cameraId, frames.Count, recognitionResults);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing frames for camera {CameraId}", cameraId);
            await HandleProcessingFailure(frames, ex.Message);
        }
    }
    
    private async Task<GpuFrameBatch> UploadFramesToGpu(List<FrameProcessingRequest> frames, 
        GpuAllocation gpuAllocation)
    {
        var batchData = new GpuFrameBatch
        {
            CameraId = frames.First().CameraId,
            Frames = new List<GpuFrame>(frames.Count),
            GpuAllocation = gpuAllocation,
            UploadedAt = DateTime.UtcNow
        };
        
        foreach (var frame in frames)
        {
            // Allocate GPU memory for frame
            var gpuFrame = await AllocateGpuFrame(frame, gpuAllocation);
            
            // Copy frame data to GPU
            await CopyFrameToGpu(frame.FrameData, gpuFrame);
            
            batchData.Frames.Add(gpuFrame);
        }
        
        return batchData;
    }
    
    private async Task<GpuFrame> AllocateGpuFrame(FrameProcessingRequest request, 
        GpuAllocation gpuAllocation)
    {
        var frameSize = request.FrameData.Length;
        var alignment = 256; // GPU memory alignment
        var alignedSize = (frameSize + alignment - 1) & ~(alignment - 1);
        
        // Allocate from GPU memory pool
        var devicePointer = await gpuAllocation.AllocateMemory(alignedSize);
        
        return new GpuFrame
        {
            RequestId = request.Id,
            DevicePointer = devicePointer,
            Width = request.Width,
            Height = request.Height,
            Channels = request.Channels,
            SizeBytes = alignedSize,
            OriginalTimestamp = request.Timestamp
        };
    }
    
    private async Task CopyFrameToGpu(byte[] frameData, GpuFrame gpuFrame)
    {
        await Task.Run(() =>
        {
            // Pin managed memory for GPU copy
            var handle = GCHandle.Alloc(frameData, GCHandleType.Pinned);
            try
            {
                var hostPointer = handle.AddrOfPinnedObject();
                
                // Asynchronous copy to GPU
                var result = CudaApi.cudaMemcpyAsync(
                    gpuFrame.DevicePointer,
                    hostPointer,
                    (uint)frameData.Length,
                    CudaMemcpyKind.HostToDevice,
                    IntPtr.Zero // Default stream
                );
                
                if (result != CudaError.Success)
                {
                    throw new InvalidOperationException($"GPU memory copy failed: {result}");
                }
                
                // Synchronize to ensure copy completion
                CudaApi.cudaStreamSynchronize(IntPtr.Zero);
            }
            finally
            {
                handle.Free();
            }
        });
    }
    
    public async Task<bool> EnqueueFrameAsync(FrameProcessingRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _requestChannel.Writer.WriteAsync(request, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue frame processing request");
            return false;
        }
    }
    
    private async Task PublishResult(FacialRecognitionResult result, CancellationToken cancellationToken)
    {
        await _publisher.Publish(new FaceDetectionEvent
        {
            CameraId = result.CameraId,
            SessionId = result.SessionId,
            DetectedFaces = result.DetectedFaces.Select(face => new DetectedFace
            {
                BoundingBox = face.BoundingBox,
                Confidence = face.Confidence,
                VisitorId = face.VisitorId,
                MatchConfidence = face.MatchConfidence,
                Features = face.Features
            }).ToList(),
            ProcessingTimeMs = result.ProcessingTimeMs,
            FrameTimestamp = result.FrameTimestamp,
            DetectedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}

// Configuration/BatchProcessingOptions.cs
public class BatchProcessingOptions
{
    public int MaxBatchSize { get; set; } = 8;
    public int MaxWaitTimeMs { get; set; } = 100;
    public int MaxQueueSize { get; set; } = 1000;
    public int MaxConcurrentBatches { get; set; } = 4;
    public bool EnableAdaptiveBatching { get; set; } = true;
    public double TargetLatencyMs { get; set; } = 200;
    public double TargetThroughputFps { get; set; } = 100;
}
```

**Deliverables**:
- [x] Batch processing engine with adaptive batching
- [x] GPU memory management for frame batches
- [x] Concurrent processing for multiple cameras
- [x] Performance monitoring and optimization
- [x] Configurable processing parameters

### 2.3 Facial Recognition Integration

#### Task 2.3.1: Face Detection Engine
**Duration**: 5 days  
**Assignee**: Senior Developer + Mid-level Developer  
**Dependencies**: Task 2.2.2  

**Implementation**:
```csharp
// Core/FacialRecognition/FaceDetectionEngine.cs
public class FaceDetectionEngine : IFaceDetectionEngine
{
    private readonly Dictionary<int, ONNXInferenceSession> _detectionSessions = new();
    private readonly Dictionary<int, ONNXInferenceSession> _recognitionSessions = new();
    private readonly IGpuResourceManager _gpuManager;
    private readonly ILogger<FaceDetectionEngine> _logger;
    private readonly FaceDetectionOptions _options;
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    
    public FaceDetectionEngine(
        IGpuResourceManager gpuManager,
        ILogger<FaceDetectionEngine> logger,
        IOptions<FaceDetectionOptions> options)
    {
        _gpuManager = gpuManager;
        _logger = logger;
        _options = options.Value;
        
        InitializeModels();
    }
    
    private void InitializeModels()
    {
        try
        {
            // Load face detection model (e.g., MTCNN, RetinaFace)
            var detectionModelPath = Path.Combine(_options.ModelsPath, "face_detection.onnx");
            var detectionOptions = new SessionOptions();
            
            if (_options.UseGpu)
            {
                // Configure CUDA execution provider
                detectionOptions.AppendExecutionProvider_CUDA(0);
                detectionOptions.EnableMemPattern = false;
                detectionOptions.EnableCpuMemArena = false;
            }
            
            var detectionSession = new InferenceSession(detectionModelPath, detectionOptions);
            _detectionSessions[0] = detectionSession; // Default session
            
            // Load face recognition model (e.g., ArcFace, FaceNet)
            var recognitionModelPath = Path.Combine(_options.ModelsPath, "face_recognition.onnx");
            var recognitionSession = new InferenceSession(recognitionModelPath, detectionOptions);
            _recognitionSessions[0] = recognitionSession;
            
            _logger.LogInformation("Initialized face detection and recognition models");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize face detection models");
            throw;
        }
    }
    
    public async Task<List<DetectedFace>> DetectFacesAsync(GpuFrame frame, 
        float confidenceThreshold = 0.7f, CancellationToken cancellationToken = default)
    {
        try
        {
            // Preprocess frame for face detection
            var preprocessedFrame = await PreprocessFrameAsync(frame);
            
            // Run face detection inference
            var detectionResults = await RunDetectionInferenceAsync(preprocessedFrame, cancellationToken);
            
            // Filter results by confidence threshold
            var detectedFaces = FilterDetectionResults(detectionResults, confidenceThreshold);
            
            // Extract face features for recognition
            var facesWithFeatures = await ExtractFaceFeatures(frame, detectedFaces, cancellationToken);
            
            _logger.LogDebug("Detected {FaceCount} faces in frame with confidence > {Threshold}", 
                detectedFaces.Count, confidenceThreshold);
            
            return facesWithFeatures;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Face detection failed for frame");
            return new List<DetectedFace>();
        }
    }
    
    private async Task<PreprocessedFrame> PreprocessFrameAsync(GpuFrame frame)
    {
        return await Task.Run(() =>
        {
            // Resize to model input size (e.g., 640x640 for RetinaFace)
            var targetWidth = _options.DetectionInputWidth;
            var targetHeight = _options.DetectionInputHeight;
            
            // Allocate preprocessed frame memory on GPU
            var preprocessedSize = targetWidth * targetHeight * 3 * sizeof(float); // RGB float32
            var preprocessedPointer = CudaApi.cudaMalloc((uint)preprocessedSize);
            
            // Launch CUDA kernel for preprocessing
            var kernelParams = new object[]
            {
                frame.DevicePointer,
                preprocessedPointer,
                frame.Width,
                frame.Height,
                targetWidth,
                targetHeight,
                _options.NormalizationMean,
                _options.NormalizationStd
            };
            
            CudaKernelLauncher.LaunchKernel("preprocess_frame_kernel", 
                new dim3((uint)((targetWidth + 31) / 32), (uint)((targetHeight + 31) / 32), 1),
                new dim3(32, 32, 1),
                kernelParams);
            
            return new PreprocessedFrame
            {
                DevicePointer = preprocessedPointer,
                Width = targetWidth,
                Height = targetHeight,
                Channels = 3,
                DataType = typeof(float)
            };
        });
    }
    
    private async Task<ONNXInferenceResult> RunDetectionInferenceAsync(PreprocessedFrame frame, 
        CancellationToken cancellationToken)
    {
        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            var session = _detectionSessions[0];
            
            // Create input tensor from GPU memory
            var inputTensor = CreateTensorFromGpuMemory(frame);
            var inputBinding = new Dictionary<string, ONNXValue>
            {
                { "input", inputTensor }
            };
            
            // Run inference
            var outputs = await Task.Run(() => session.Run(inputBinding), cancellationToken);
            
            return new ONNXInferenceResult
            {
                Outputs = outputs,
                InferenceTimeMs = 0 // Measure actual time
            };
        }
        finally
        {
            _sessionLock.Release();
        }
    }
    
    private List<DetectedFace> FilterDetectionResults(ONNXInferenceResult results, float confidenceThreshold)
    {
        var detectedFaces = new List<DetectedFace>();
        
        // Parse ONNX output tensors (format depends on model)
        var boundingBoxes = results.Outputs["boxes"].AsTensor<float>();
        var confidences = results.Outputs["scores"].AsTensor<float>();
        var landmarks = results.Outputs["landmarks"].AsTensor<float>();
        
        var numDetections = boundingBoxes.Dimensions[0];
        
        for (int i = 0; i < numDetections; i++)
        {
            var confidence = confidences[i];
            
            if (confidence >= confidenceThreshold)
            {
                var face = new DetectedFace
                {
                    BoundingBox = new BoundingBox
                    {
                        X = (int)boundingBoxes[i, 0],
                        Y = (int)boundingBoxes[i, 1],
                        Width = (int)(boundingBoxes[i, 2] - boundingBoxes[i, 0]),
                        Height = (int)(boundingBoxes[i, 3] - boundingBoxes[i, 1])
                    },
                    Confidence = confidence,
                    Landmarks = ExtractFacialLandmarks(landmarks, i),
                    DetectedAt = DateTime.UtcNow
                };
                
                detectedFaces.Add(face);
            }
        }
        
        // Apply Non-Maximum Suppression to remove overlapping detections
        return ApplyNonMaximumSuppression(detectedFaces, _options.NmsThreshold);
    }
    
    private async Task<List<DetectedFace>> ExtractFaceFeatures(GpuFrame originalFrame, 
        List<DetectedFace> faces, CancellationToken cancellationToken)
    {
        var facesWithFeatures = new List<DetectedFace>();
        
        foreach (var face in faces)
        {
            try
            {
                // Extract face region from original frame
                var faceRegion = await ExtractFaceRegion(originalFrame, face.BoundingBox);
                
                // Preprocess for recognition model
                var preprocessedFace = await PreprocessFaceForRecognition(faceRegion);
                
                // Extract feature vector
                var features = await ExtractFeatureVector(preprocessedFace, cancellationToken);
                
                face.FeatureVector = features;
                face.FeatureExtractionTime = DateTime.UtcNow;
                
                facesWithFeatures.Add(face);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract features for detected face");
                
                // Add face without features (detection still valid)
                facesWithFeatures.Add(face);
            }
        }
        
        return facesWithFeatures;
    }
    
    private async Task<float[]> ExtractFeatureVector(PreprocessedFrame faceRegion, 
        CancellationToken cancellationToken)
    {
        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            var session = _recognitionSessions[0];
            
            var inputTensor = CreateTensorFromGpuMemory(faceRegion);
            var inputBinding = new Dictionary<string, ONNXValue>
            {
                { "input", inputTensor }
            };
            
            var outputs = await Task.Run(() => session.Run(inputBinding), cancellationToken);
            var featureTensor = outputs["output"].AsTensor<float>();
            
            // Convert to array and normalize
            var features = featureTensor.ToArray();
            return NormalizeFeatureVector(features);
        }
        finally
        {
            _sessionLock.Release();
        }
    }
    
    private float[] NormalizeFeatureVector(float[] features)
    {
        // L2 normalization
        var magnitude = Math.Sqrt(features.Sum(f => f * f));
        
        if (magnitude > 0)
        {
            for (int i = 0; i < features.Length; i++)
            {
                features[i] /= (float)magnitude;
            }
        }
        
        return features;
    }
}

// Core/FacialRecognition/VisitorMatcher.cs
public class VisitorMatcher : IVisitorMatcher
{
    private readonly Dictionary<int, VisitorFaceProfile> _visitorProfiles = new();
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VisitorMatcher> _logger;
    private readonly SemaphoreSlim _profileLock = new(1, 1);
    private readonly Timer _profileUpdateTimer;
    
    public VisitorMatcher(IUnitOfWork unitOfWork, ILogger<VisitorMatcher> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        
        // Update visitor profiles every 5 minutes
        _profileUpdateTimer = new Timer(UpdateVisitorProfiles, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
    }
    
    public async Task<List<VisitorMatch>> FindMatchesAsync(List<DetectedFace> faces, 
        float matchThreshold = 0.75f, CancellationToken cancellationToken = default)
    {
        var matches = new List<VisitorMatch>();
        
        await _profileLock.WaitAsync(cancellationToken);
        try
        {
            foreach (var face in faces.Where(f => f.FeatureVector != null))
            {
                var bestMatch = await FindBestMatch(face.FeatureVector, matchThreshold);
                
                if (bestMatch != null)
                {
                    matches.Add(new VisitorMatch
                    {
                        DetectedFace = face,
                        VisitorId = bestMatch.VisitorId,
                        VisitorName = bestMatch.VisitorName,
                        MatchConfidence = bestMatch.Similarity,
                        MatchedAt = DateTime.UtcNow
                    });
                    
                    _logger.LogDebug("Matched face to visitor {VisitorId} with confidence {Confidence:F3}", 
                        bestMatch.VisitorId, bestMatch.Similarity);
                }
            }
        }
        finally
        {
            _profileLock.Release();
        }
        
        return matches;
    }
    
    private async Task<MatchResult?> FindBestMatch(float[] faceFeatures, float threshold)
    {
        var bestMatch = default(MatchResult);
        var bestSimilarity = 0f;
        
        // Use parallel processing for large visitor databases
        var matchTasks = _visitorProfiles.Values.Select(async profile =>
        {
            var similarity = await CalculateSimilarity(faceFeatures, profile.AverageFeatures);
            
            if (similarity > threshold && similarity > bestSimilarity)
            {
                return new MatchResult
                {
                    VisitorId = profile.VisitorId,
                    VisitorName = profile.VisitorName,
                    Similarity = similarity
                };
            }
            
            return null;
        });
        
        var results = await Task.WhenAll(matchTasks);
        return results.Where(r => r != null).OrderByDescending(r => r.Similarity).FirstOrDefault();
    }
    
    private async Task<float> CalculateSimilarity(float[] features1, float[] features2)
    {
        return await Task.Run(() =>
        {
            // Cosine similarity
            var dotProduct = 0f;
            var magnitude1 = 0f;
            var magnitude2 = 0f;
            
            for (int i = 0; i < Math.Min(features1.Length, features2.Length); i++)
            {
                dotProduct += features1[i] * features2[i];
                magnitude1 += features1[i] * features1[i];
                magnitude2 += features2[i] * features2[i];
            }
            
            magnitude1 = (float)Math.Sqrt(magnitude1);
            magnitude2 = (float)Math.Sqrt(magnitude2);
            
            if (magnitude1 == 0f || magnitude2 == 0f)
                return 0f;
            
            return dotProduct / (magnitude1 * magnitude2);
        });
    }
    
    private async void UpdateVisitorProfiles(object? state)
    {
        try
        {
            _logger.LogDebug("Updating visitor face profiles");
            
            // Get visitors with face data from last 30 days
            var visitors = await _unitOfWork.Visitors.GetVisitorsWithRecentFaceDataAsync(30);
            
            await _profileLock.WaitAsync();
            try
            {
                var updatedProfiles = new Dictionary<int, VisitorFaceProfile>();
                
                foreach (var visitor in visitors)
                {
                    var profile = await BuildVisitorProfile(visitor);
                    if (profile != null)
                    {
                        updatedProfiles[visitor.Id] = profile;
                    }
                }
                
                // Replace profiles atomically
                _visitorProfiles.Clear();
                foreach (var kvp in updatedProfiles)
                {
                    _visitorProfiles[kvp.Key] = kvp.Value;
                }
                
                _logger.LogInformation("Updated {Count} visitor face profiles", _visitorProfiles.Count);
            }
            finally
            {
                _profileLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update visitor profiles");
        }
    }
    
    private async Task<VisitorFaceProfile?> BuildVisitorProfile(Visitor visitor)
    {
        // Get all face detection records for this visitor
        var faceDetections = await _unitOfWork.FaceDetections
            .GetByVisitorIdAsync(visitor.Id, TimeSpan.FromDays(30));
        
        if (!faceDetections.Any())
            return null;
        
        // Calculate average feature vector from multiple detections
        var featureVectors = faceDetections
            .Where(fd => fd.FeatureVector != null)
            .Select(fd => fd.FeatureVector)
            .ToList();
        
        if (!featureVectors.Any())
            return null;
        
        var averageFeatures = CalculateAverageFeatures(featureVectors);
        
        return new VisitorFaceProfile
        {
            VisitorId = visitor.Id,
            VisitorName = $"{visitor.FirstName} {visitor.LastName}",
            AverageFeatures = averageFeatures,
            SampleCount = featureVectors.Count,
            LastUpdated = DateTime.UtcNow,
            Confidence = CalculateProfileConfidence(featureVectors)
        };
    }
}
```

**Deliverables**:
- [x] ONNX model integration for face detection
- [x] GPU-accelerated inference pipeline
- [x] Feature extraction and normalization
- [x] Visitor matching with similarity scoring
- [x] Profile management and caching

---

## Phase 3: Production Readiness (Weeks 9-12)

### 3.1 Health Monitoring & Observability

#### Task 3.1.1: Comprehensive Health Checks
**Duration**: 3 days  
**Assignee**: Mid-level Developer  
**Dependencies**: Phase 2 completion  

**Implementation**:
```csharp
// Infrastructure/HealthChecks/GrabberHealthCheck.cs
public class GrabberHealthCheck : IHealthCheck
{
    private readonly ICameraSessionManager _sessionManager;
    private readonly IGpuResourceManager _gpuManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GrabberHealthCheck> _logger;
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var healthData = new Dictionary<string, object>();
        var isHealthy = true;
        var issues = new List<string>();
        
        try
        {
            // Check GPU availability
            var gpuHealth = await CheckGpuHealth();
            healthData["gpu"] = gpuHealth;
            if (!gpuHealth.IsHealthy)
            {
                isHealthy = false;
                issues.Add($"GPU issues: {gpuHealth.Issues}");
            }
            
            // Check active camera sessions
            var sessionHealth = await CheckCameraSessions();
            healthData["camera_sessions"] = sessionHealth;
            if (sessionHealth.FailedSessions > 0)
            {
                issues.Add($"{sessionHealth.FailedSessions} camera sessions failed");
            }
            
            // Check message queue connectivity
            var queueHealth = await CheckMessageQueues();
            healthData["message_queues"] = queueHealth;
            if (!queueHealth.IsConnected)
            {
                isHealthy = false;
                issues.Add("Message queue disconnected");
            }
            
            // Check system resources
            var systemHealth = await CheckSystemResources();
            healthData["system"] = systemHealth;
            if (systemHealth.CpuUsage > 90 || systemHealth.MemoryUsage > 90)
            {
                issues.Add($"High resource usage: CPU {systemHealth.CpuUsage}%, RAM {systemHealth.MemoryUsage}%");
            }
            
            // Check facial recognition models
            var modelHealth = await CheckFaceRecognitionModels();
            healthData["face_recognition"] = modelHealth;
            if (!modelHealth.IsLoaded)
            {
                isHealthy = false;
                issues.Add("Facial recognition models not loaded");
            }
            
            var status = isHealthy ? HealthStatus.Healthy : 
                        issues.Any(i => i.Contains("GPU") || i.Contains("queue")) ? HealthStatus.Unhealthy : 
                        HealthStatus.Degraded;
            
            return new HealthCheckResult(status, 
                isHealthy ? "All systems operational" : string.Join("; ", issues), 
                null, healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
    
    private async Task<GpuHealthStatus> CheckGpuHealth()
    {
        var devices = await _gpuManager.GetDevicesAsync();
        var healthyDevices = 0;
        var issues = new List<string>();
        
        foreach (var device in devices)
        {
            var metrics = await _gpuManager.GetDeviceMetricsAsync(device.Id);
            
            if (metrics.Temperature > 85) // GPU too hot
            {
                issues.Add($"GPU {device.Id} overheating: {metrics.Temperature}°C");
            }
            else if (metrics.MemoryUtilization > 0.95) // Memory exhausted
            {
                issues.Add($"GPU {device.Id} memory exhausted: {metrics.MemoryUtilization:P1}");
            }
            else
            {
                healthyDevices++;
            }
        }
        
        return new GpuHealthStatus
        {
            IsHealthy = healthyDevices > 0,
            TotalDevices = devices.Count(),
            HealthyDevices = healthyDevices,
            Issues = string.Join(", ", issues)
        };
    }
    
    private async Task<CameraSessionHealth> CheckCameraSessions()
    {
        var sessions = await _sessionManager.GetActiveSessionsAsync();
        
        return new CameraSessionHealth
        {
            TotalSessions = sessions.Count(),
            ActiveSessions = sessions.Count(s => s.Status == CameraSessionStatus.Active),
            FailedSessions = sessions.Count(s => s.Status == CameraSessionStatus.Error),
            AverageProcessingTime = sessions.Where(s => s.AverageProcessingTimeMs > 0)
                                           .Average(s => s.AverageProcessingTimeMs)
        };
    }
}

// Infrastructure/Metrics/MetricsCollectionService.cs
public class MetricsCollectionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MetricsCollectionService> _logger;
    private readonly Timer _metricsTimer;
    
    // Prometheus metrics
    private static readonly Gauge ActiveCameraSessions = Metrics
        .CreateGauge("grabber_active_camera_sessions", "Number of active camera sessions");
        
    private static readonly Histogram FrameProcessingDuration = Metrics
        .CreateHistogram("grabber_frame_processing_duration_seconds", 
            "Frame processing duration in seconds");
            
    private static readonly Counter ProcessedFramesTotal = Metrics
        .CreateCounter("grabber_processed_frames_total", 
            "Total number of processed frames", new[] { "camera_id", "result" });
            
    private static readonly Gauge GpuUtilization = Metrics
        .CreateGauge("grabber_gpu_utilization_percent", 
            "GPU utilization percentage", new[] { "gpu_id" });
            
    private static readonly Counter FacesDetectedTotal = Metrics
        .CreateCounter("grabber_faces_detected_total", 
            "Total number of faces detected", new[] { "camera_id" });
            
    private static readonly Histogram VisitorMatchingDuration = Metrics
        .CreateHistogram("grabber_visitor_matching_duration_seconds",
            "Visitor matching duration in seconds");
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectMetrics();
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Metrics collection failed");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
    
    private async Task CollectMetrics()
    {
        using var scope = _serviceProvider.CreateScope();
        
        // Collect camera session metrics
        var sessionManager = scope.ServiceProvider.GetRequiredService<ICameraSessionManager>();
        var sessions = await sessionManager.GetActiveSessionsAsync();
        
        ActiveCameraSessions.Set(sessions.Count(s => s.Status == CameraSessionStatus.Active));
        
        // Collect GPU metrics
        var gpuManager = scope.ServiceProvider.GetRequiredService<IGpuResourceManager>();
        var devices = await gpuManager.GetDevicesAsync();
        
        foreach (var device in devices)
        {
            var metrics = await gpuManager.GetDeviceMetricsAsync(device.Id);
            GpuUtilization.WithLabels(device.Id.ToString()).Set(metrics.GpuUtilization * 100);
        }
    }
    
    // Static methods for recording metrics from other components
    public static void RecordFrameProcessing(string cameraId, double durationSeconds, bool success)
    {
        FrameProcessingDuration.Observe(durationSeconds);
        ProcessedFramesTotal.WithLabels(cameraId, success ? "success" : "failure").Inc();
    }
    
    public static void RecordFaceDetection(string cameraId, int faceCount)
    {
        FacesDetectedTotal.WithLabels(cameraId).Inc(faceCount);
    }
    
    public static void RecordVisitorMatching(double durationSeconds)
    {
        VisitorMatchingDuration.Observe(durationSeconds);
    }
}
```

**Deliverables**:
- [x] Multi-level health checks (GPU, sessions, queues, system)
- [x] Prometheus metrics collection
- [x] Performance monitoring dashboards
- [x] Alerting thresholds configuration

#### Task 3.1.2: Structured Logging Implementation
**Duration**: 2 days  
**Assignee**: Mid-level Developer  
**Dependencies**: Task 3.1.1  

**Implementation**:
```csharp
// Infrastructure/Logging/StructuredLoggingExtensions.cs
public static class StructuredLoggingExtensions
{
    public static ILoggingBuilder AddGrabberStructuredLogging(this ILoggingBuilder builder, 
        IConfiguration configuration)
    {
        builder.ClearProviders();
        
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty("Service", "GrabberService")
            .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version?.ToString())
            .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .WriteTo.Console(new JsonFormatter())
            .WriteTo.File(new JsonFormatter(), 
                path: "logs/grabber-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(configuration.GetConnectionString("Elasticsearch")))
            {
                AutoRegisterTemplate = true,
                IndexFormat = "grabber-logs-{0:yyyy.MM.dd}",
                BatchAction = ElasticOpType.Index
            });
        
        Log.Logger = loggerConfig.CreateLogger();
        builder.AddSerilog();
        
        return builder;
    }
}

// Application/Logging/LoggingConstants.cs
public static class LoggingConstants
{
    public const string CameraOperation = "CameraOperation";
    public const string FrameProcessing = "FrameProcessing";
    public const string FaceDetection = "FaceDetection";
    public const string VisitorMatching = "VisitorMatching";
    public const string GpuOperation = "GpuOperation";
    public const string MessageQueue = "MessageQueue";
    public const string HealthCheck = "HealthCheck";
    public const string Performance = "Performance";
    
    public static class Events
    {
        public const int CameraStarted = 1001;
        public const int CameraStopped = 1002;
        public const int CameraError = 1003;
        public const int FrameProcessed = 2001;
        public const int FrameDropped = 2002;
        public const int FaceDetected = 3001;
        public const int VisitorMatched = 3002;
        public const int GpuAllocation = 4001;
        public const int GpuDeallocation = 4002;
        public const int MessagePublished = 5001;
        public const int MessageConsumed = 5002;
        public const int HealthCheckFailed = 6001;
        public const int PerformanceAlert = 7001;
    }
}

// Application/Logging/GrabberLogger.cs
public static class GrabberLogger
{
    public static void LogCameraStarted(this ILogger logger, int cameraId, string sessionId, 
        CameraConfiguration config)
    {
        logger.LogInformation(LoggingConstants.Events.CameraStarted,
            "Camera {CameraId} started with session {SessionId}. Resolution: {Resolution}, FPS: {FrameRate}, FR: {FacialRecognition}",
            cameraId, sessionId, $"{config.ResolutionWidth}x{config.ResolutionHeight}", 
            config.FrameRate, config.EnableFacialRecognition);
    }
    
    public static void LogFrameProcessed(this ILogger logger, int cameraId, string sessionId, 
        double processingTimeMs, int facesDetected, int visitorsMatched)
    {
        logger.LogDebug(LoggingConstants.Events.FrameProcessed,
            "Frame processed for camera {CameraId} session {SessionId}. " +
            "Time: {ProcessingTime}ms, Faces: {FacesDetected}, Matches: {VisitorMatches}",
            cameraId, sessionId, processingTimeMs, facesDetected, visitorsMatched);
    }
    
    public static void LogVisitorMatched(this ILogger logger, int cameraId, int visitorId, 
        string visitorName, float confidence, DateTime detectedAt)
    {
        logger.LogInformation(LoggingConstants.Events.VisitorMatched,
            "Visitor matched: {VisitorName} (ID: {VisitorId}) at camera {CameraId} " +
            "with confidence {Confidence:F3} at {DetectedAt}",
            visitorName, visitorId, cameraId, confidence, detectedAt);
    }
    
    public static void LogPerformanceAlert(this ILogger logger, string component, 
        string metric, double value, double threshold, string unit)
    {
        logger.LogWarning(LoggingConstants.Events.PerformanceAlert,
            "Performance alert: {Component} {Metric} is {Value:F2}{Unit} (threshold: {Threshold:F2}{Unit})",
            component, metric, value, unit, threshold, unit);
    }
    
    public static void LogGpuResourceAllocation(this ILogger logger, int cameraId, int gpuId, 
        int memoryMB, string allocationId)
    {
        logger.LogDebug(LoggingConstants.Events.GpuAllocation,
            "GPU resources allocated: Camera {CameraId} -> GPU {GpuId}, Memory: {MemoryMB}MB, Allocation: {AllocationId}",
            cameraId, gpuId, memoryMB, allocationId);
    }
}
```

**Deliverables**:
- [x] Structured logging with Serilog and JSON formatting
- [x] Elasticsearch integration for log aggregation
- [x] Contextual logging extensions for all components
- [x] Performance and security event tracking

### 3.2 Error Handling & Recovery

#### Task 3.2.1: Advanced Retry Mechanisms
**Duration**: 3 days  
**Assignee**: Senior Developer  
**Dependencies**: Task 3.1.2  

**Implementation**:
```csharp
// Infrastructure/Resilience/RetryPolicyFactory.cs
public static class RetryPolicyFactory
{
    public static IRetryPolicy CreateCameraConnectionRetry()
    {
        return Policy
            .Handle<CameraConnectionException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + 
                                                       TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning("Camera connection retry attempt {RetryCount} in {Delay}ms. Error: {Error}",
                        retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message);
                });
    }
    
    public static IRetryPolicy CreateGpuOperationRetry()
    {
        return Policy
            .Handle<CudaException>()
            .Or<OutOfMemoryException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(500 * retryAttempt),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning("GPU operation retry attempt {RetryCount}. Error: {Error}",
                        retryCount, outcome.Exception?.Message);
                    
                    // Trigger GPU memory cleanup before retry
                    if (outcome.Exception is OutOfMemoryException)
                    {
                        var gpuManager = context.GetService<IGpuResourceManager>();
                        _ = Task.Run(() => gpuManager?.TriggerGarbageCollectionAsync());
                    }
                });
    }
    
    public static IRetryPolicy CreateMessageQueueRetry()
    {
        return Policy
            .Handle<RabbitMQConnectionException>()
            .Or<BrokerUnreachableException>()
            .WaitAndRetryForeverAsync(
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, Math.Min(retryAttempt, 5)))),
                onRetry: (exception, retryCount, timespan) =>
                {
                    Log.Warning("Message queue connection retry {RetryCount} in {Delay}s. Error: {Error}",
                        retryCount, timespan.TotalSeconds, exception.Message);
                });
    }
}

// Infrastructure/Resilience/CircuitBreakerService.cs
public class CircuitBreakerService
{
    private readonly Dictionary<string, ICircuitBreaker> _circuitBreakers = new();
    private readonly ILogger<CircuitBreakerService> _logger;
    
    public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
    {
        _logger = logger;
        InitializeCircuitBreakers();
    }
    
    private void InitializeCircuitBreakers()
    {
        // Camera connection circuit breaker
        _circuitBreakers["camera_connection"] = Policy
            .Handle<CameraConnectionException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(2),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError("Camera connection circuit breaker OPENED. Duration: {Duration}s. Last error: {Error}",
                        duration.TotalSeconds, exception.Message);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Camera connection circuit breaker CLOSED");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Camera connection circuit breaker HALF-OPEN");
                });
        
        // GPU processing circuit breaker
        _circuitBreakers["gpu_processing"] = Policy
            .Handle<CudaException>()
            .Or<OutOfMemoryException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError("GPU processing circuit breaker OPENED. Duration: {Duration}s. Last error: {Error}",
                        duration.TotalSeconds, exception.Message);
                },
                onReset: () =>
                {
                    _logger.LogInformation("GPU processing circuit breaker CLOSED");
                });
        
        // Facial recognition circuit breaker
        _circuitBreakers["face_recognition"] = Policy
            .Handle<ONNXException>()
            .Or<ModelLoadException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(5),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError("Facial recognition circuit breaker OPENED. Duration: {Duration}s. Last error: {Error}",
                        duration.TotalSeconds, exception.Message);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Facial recognition circuit breaker CLOSED");
                });
    }
    
    public async Task<T> ExecuteAsync<T>(string circuitBreakerName, Func<Task<T>> operation)
    {
        if (_circuitBreakers.TryGetValue(circuitBreakerName, out var circuitBreaker))
        {
            return await circuitBreaker.ExecuteAsync(operation);
        }
        
        return await operation();
    }
    
    public CircuitBreakerState GetState(string circuitBreakerName)
    {
        return _circuitBreakers.TryGetValue(circuitBreakerName, out var cb) ? cb.CircuitState : CircuitBreakerState.Closed;
    }
}
```

**Deliverables**:
- [x] Exponential backoff retry policies for all operations
- [x] Circuit breaker patterns for critical components
- [x] Graceful degradation strategies
- [x] Error correlation and root cause analysis

#### Task 3.2.2: Disaster Recovery Implementation
**Duration**: 4 days  
**Assignee**: Senior Developer + Mid-level Developer  
**Dependencies**: Task 3.2.1  

**Implementation**:
```csharp
// Infrastructure/DisasterRecovery/RecoveryOrchestrator.cs
public class RecoveryOrchestrator : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecoveryOrchestrator> _logger;
    private readonly RecoveryConfiguration _config;
    private readonly Dictionary<string, DateTime> _lastRecoveryAttempts = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorAndRecover(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recovery orchestrator error");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
    
    private async Task MonitorAndRecover(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        // Check GPU health and recover if needed
        await CheckAndRecoverGpuResources(scope, cancellationToken);
        
        // Check camera sessions and recover failed ones
        await CheckAndRecoverCameraSessions(scope, cancellationToken);
        
        // Check message queue connectivity
        await CheckAndRecoverMessageQueue(scope, cancellationToken);
        
        // Check facial recognition models
        await CheckAndRecoverFaceRecognitionModels(scope, cancellationToken);
    }
    
    private async Task CheckAndRecoverGpuResources(IServiceScope scope, CancellationToken cancellationToken)
    {
        var gpuManager = scope.ServiceProvider.GetRequiredService<IGpuResourceManager>();
        var devices = await gpuManager.GetDevicesAsync();
        
        foreach (var device in devices)
        {
            try
            {
                var metrics = await gpuManager.GetDeviceMetricsAsync(device.Id);
                
                // Check for GPU hang or error state
                if (metrics.IsHung || metrics.ErrorCount > _config.MaxGpuErrors)
                {
                    _logger.LogWarning("GPU {DeviceId} appears to be hung or in error state. Attempting recovery...", 
                        device.Id);
                    
                    await RecoverGpuDevice(gpuManager, device.Id, cancellationToken);
                }
                
                // Check for memory leaks
                if (metrics.MemoryUtilization > 0.95 && !HasRecentActivity(device.Id))
                {
                    _logger.LogWarning("GPU {DeviceId} memory utilization high with no recent activity. Triggering cleanup...", 
                        device.Id);
                    
                    await gpuManager.TriggerGarbageCollectionAsync(device.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking GPU {DeviceId} health", device.Id);
            }
        }
    }
    
    private async Task RecoverGpuDevice(IGpuResourceManager gpuManager, int deviceId, CancellationToken cancellationToken)
    {
        var recoveryKey = $"gpu_{deviceId}";
        
        // Prevent too frequent recovery attempts
        if (_lastRecoveryAttempts.TryGetValue(recoveryKey, out var lastAttempt) &&
            DateTime.UtcNow - lastAttempt < TimeSpan.FromMinutes(5))
        {
            return;
        }
        
        _lastRecoveryAttempts[recoveryKey] = DateTime.UtcNow;
        
        try
        {
            // Stop all allocations on this device
            await gpuManager.StopAllAllocationsAsync(deviceId);
            
            // Reset GPU device
            await gpuManager.ResetDeviceAsync(deviceId);
            
            // Wait for device to stabilize
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            
            // Verify recovery
            var metrics = await gpuManager.GetDeviceMetricsAsync(deviceId);
            if (metrics.IsHealthy)
            {
                _logger.LogInformation("Successfully recovered GPU {DeviceId}", deviceId);
                
                // Restart camera sessions that were using this GPU
                await RestartCameraSessionsOnGpu(deviceId, cancellationToken);
            }
            else
            {
                _logger.LogError("Failed to recover GPU {DeviceId}. Device still unhealthy", deviceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPU {DeviceId} recovery failed", deviceId);
        }
    }
    
    private async Task CheckAndRecoverCameraSessions(IServiceScope scope, CancellationToken cancellationToken)
    {
        var sessionManager = scope.ServiceProvider.GetRequiredService<ICameraSessionManager>();
        var sessions = await sessionManager.GetActiveSessionsAsync();
        
        var failedSessions = sessions.Where(s => 
            s.Status == CameraSessionStatus.Error || 
            (s.Status == CameraSessionStatus.Active && 
             DateTime.UtcNow - s.LastFrameProcessed > TimeSpan.FromMinutes(5))).ToList();
        
        foreach (var session in failedSessions)
        {
            try
            {
                _logger.LogInformation("Attempting to recover camera session {SessionId} for camera {CameraId}",
                    session.Id, session.CameraId);
                
                // Stop the current session
                await sessionManager.StopSessionAsync(session.CameraId);
                
                // Wait before restart
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                
                // Create new session with same configuration
                var newSession = await sessionManager.CreateSessionAsync(session.CameraId, 
                    $"recovery_{Guid.NewGuid()}", session.Configuration);
                
                if (newSession != null)
                {
                    var started = await sessionManager.StartSessionAsync(session.CameraId);
                    if (started)
                    {
                        _logger.LogInformation("Successfully recovered camera session for camera {CameraId}", 
                            session.CameraId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover camera session for camera {CameraId}", session.CameraId);
            }
        }
    }
    
    private async Task BackupCriticalData(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            
            // Backup recent face detection results
            var recentDetections = await unitOfWork.FaceDetections
                .GetRecentDetectionsAsync(TimeSpan.FromHours(1));
                
            var backupData = new
            {
                Timestamp = DateTime.UtcNow,
                FaceDetections = recentDetections.Select(fd => new
                {
                    fd.Id,
                    fd.CameraId,
                    fd.VisitorId,
                    fd.Confidence,
                    fd.DetectedAt,
                    fd.BoundingBox
                }),
                SystemMetrics = await CollectSystemMetrics()
            };
            
            var backupJson = JsonSerializer.Serialize(backupData, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            
            var backupPath = Path.Combine(_config.BackupDirectory, 
                $"critical_data_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.json");
            
            await File.WriteAllTextAsync(backupPath, backupJson, cancellationToken);
            
            _logger.LogInformation("Critical data backed up to {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup critical data");
        }
    }
    
    private async Task<object> CollectSystemMetrics()
    {
        return new
        {
            MachineName = Environment.MachineName,
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet,
            TotalProcessorTime = Process.GetCurrentProcess().TotalProcessorTime,
            Timestamp = DateTime.UtcNow
        };
    }
}

// Configuration/RecoveryConfiguration.cs
public class RecoveryConfiguration
{
    public int MaxGpuErrors { get; set; } = 10;
    public TimeSpan GpuRecoveryInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan SessionRecoveryTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public string BackupDirectory { get; set; } = "backups";
    public bool EnableAutomaticRecovery { get; set; } = true;
    public bool EnableDataBackup { get; set; } = true;
    public TimeSpan BackupInterval { get; set; } = TimeSpan.FromHours(1);
}
```

**Deliverables**:
- [x] Automatic failure detection and recovery
- [x] GPU device recovery and session restart
- [x] Critical data backup and restore
- [x] Recovery orchestration and monitoring

### 3.3 Security Hardening

#### Task 3.3.1: API Security Implementation
**Duration**: 3 days  
**Assignee**: Senior Developer  
**Dependencies**: Task 3.2.2  

**Implementation**:
```csharp
// Infrastructure/Security/ApiKeyAuthenticationHandler.cs
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private const string API_KEY_HEADER = "X-API-Key";
    private readonly IApiKeyValidationService _apiKeyService;
    
    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock,
        IApiKeyValidationService apiKeyService)
        : base(options, logger, encoder, clock)
    {
        _apiKeyService = apiKeyService;
    }
    
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(API_KEY_HEADER))
        {
            return AuthenticateResult.Fail("API Key missing");
        }
        
        var apiKey = Request.Headers[API_KEY_HEADER].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            return AuthenticateResult.Fail("API Key empty");
        }
        
        var validationResult = await _apiKeyService.ValidateAsync(apiKey);
        if (!validationResult.IsValid)
        {
            Logger.LogWarning("Invalid API key attempt from {RemoteIp}: {Reason}",
                Context.Connection.RemoteIpAddress, validationResult.Reason);
            return AuthenticateResult.Fail("Invalid API Key");
        }
        
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, validationResult.ServiceName),
            new Claim(ClaimTypes.NameIdentifier, validationResult.ServiceId),
            new Claim("api_key_id", validationResult.ApiKeyId),
            new Claim("permissions", string.Join(",", validationResult.Permissions))
        };
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}

// Infrastructure/Security/RequestValidationMiddleware.cs
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;
    private readonly SecurityConfiguration _config;
    private readonly MemoryCache _rateLimitCache;
    
    public RequestValidationMiddleware(RequestDelegate next, 
        ILogger<RequestValidationMiddleware> logger,
        IOptions<SecurityConfiguration> config)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
        _rateLimitCache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 10000
        });
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Rate limiting
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!await CheckRateLimit(clientIp))
        {
            _logger.LogWarning("Rate limit exceeded for IP {ClientIp}", clientIp);
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Rate limit exceeded");
            return;
        }
        
        // Request size validation
        if (context.Request.ContentLength > _config.MaxRequestSizeBytes)
        {
            _logger.LogWarning("Request size {Size} exceeds limit {Limit} from IP {ClientIp}",
                context.Request.ContentLength, _config.MaxRequestSizeBytes, clientIp);
            context.Response.StatusCode = 413;
            await context.Response.WriteAsync("Request too large");
            return;
        }
        
        // Input validation for critical endpoints
        if (IsCriticalEndpoint(context.Request.Path))
        {
            var validationResult = await ValidateRequestInput(context);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid request input from IP {ClientIp}: {Errors}",
                    clientIp, string.Join(", ", validationResult.Errors));
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid request input");
                return;
            }
        }
        
        await _next(context);
    }
    
    private async Task<bool> CheckRateLimit(string clientIp)
    {
        var cacheKey = $"rate_limit_{clientIp}";
        var requestCount = _rateLimitCache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            entry.Size = 1;
            return 0;
        });
        
        requestCount++;
        _rateLimitCache.Set(cacheKey, requestCount, TimeSpan.FromMinutes(1));
        
        return requestCount <= _config.MaxRequestsPerMinute;
    }
    
    private bool IsCriticalEndpoint(string path)
    {
        var criticalPaths = new[]
        {
            "/api/cameras/start",
            "/api/cameras/stop",
            "/api/frames/process",
            "/api/gpu/allocate"
        };
        
        return criticalPaths.Any(cp => path.StartsWith(cp, StringComparison.OrdinalIgnoreCase));
    }
    
    private async Task<ValidationResult> ValidateRequestInput(HttpContext context)
    {
        var errors = new List<string>();
        
        // Validate JSON structure for POST requests
        if (context.Request.Method == "POST" && 
            context.Request.ContentType?.Contains("application/json") == true)
        {
            try
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;
                
                if (!string.IsNullOrEmpty(body))
                {
                    JsonDocument.Parse(body);
                }
            }
            catch (JsonException ex)
            {
                errors.Add($"Invalid JSON: {ex.Message}");
            }
        }
        
        // Validate query parameters
        foreach (var param in context.Request.Query)
        {
            if (param.Key.Length > 100 || param.Value.ToString().Length > 1000)
            {
                errors.Add($"Parameter {param.Key} too long");
            }
            
            // Check for potential injection patterns
            if (ContainsInjectionPattern(param.Value))
            {
                errors.Add($"Invalid characters in parameter {param.Key}");
            }
        }
        
        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
    
    private bool ContainsInjectionPattern(string value)
    {
        var suspiciousPatterns = new[]
        {
            "<script", "javascript:", "onload=", "onerror=",
            "SELECT", "INSERT", "UPDATE", "DELETE", "DROP",
            "../", "..\\", "%2e%2e", "%252e%252e"
        };
        
        return suspiciousPatterns.Any(pattern => 
            value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
```

**Deliverables**:
- [x] API key authentication with role-based permissions
- [x] Request validation and sanitization middleware
- [x] Rate limiting and DDoS protection
- [x] Security audit logging and monitoring

---

## Phase 4: Deployment & Operations (Weeks 13-16)

### 4.1 Production Deployment

#### Task 4.1.1: Container Orchestration
**Duration**: 4 days  
**Assignee**: DevOps Engineer + Senior Developer  
**Dependencies**: Phase 3 completion  

**Kubernetes Deployment Configuration**:
```yaml
# k8s/namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: vms-grabber
  labels:
    name: vms-grabber
    purpose: camera-processing
---
# k8s/configmap.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: grabber-config
  namespace: vms-grabber
data:
  appsettings.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft": "Warning",
          "System": "Warning"
        }
      },
      "ConnectionStrings": {
        "DefaultConnection": "Server=postgres-service;Database=VmsGrabber;Username=grabber_user;Password=${DB_PASSWORD}",
        "RabbitMQ": "amqp://vms_user:${RABBITMQ_PASSWORD}@rabbitmq-service:5672/vms",
        "Elasticsearch": "http://elasticsearch-service:9200"
      },
      "BatchProcessing": {
        "MaxBatchSize": 8,
        "MaxWaitTimeMs": 100,
        "MaxQueueSize": 1000,
        "EnableAdaptiveBatching": true
      },
      "FaceDetection": {
        "ModelsPath": "/app/models",
        "UseGpu": true,
        "ConfidenceThreshold": 0.7,
        "DetectionInputWidth": 640,
        "DetectionInputHeight": 640
      },
      "Security": {
        "MaxRequestsPerMinute": 1000,
        "MaxRequestSizeBytes": 10485760,
        "EnableApiKeyAuth": true
      },
      "Recovery": {
        "MaxGpuErrors": 10,
        "EnableAutomaticRecovery": true,
        "BackupInterval": "01:00:00"
      }
    }
---
# k8s/secret.yaml
apiVersion: v1
kind: Secret
metadata:
  name: grabber-secrets
  namespace: vms-grabber
type: Opaque
data:
  DB_PASSWORD: <base64-encoded-password>
  RABBITMQ_PASSWORD: <base64-encoded-password>
  API_KEY: <base64-encoded-api-key>
---
# k8s/persistent-volume.yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: grabber-models-pvc
  namespace: vms-grabber
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: fast-ssd
  resources:
    requests:
      storage: 10Gi
---
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: grabber-service
  namespace: vms-grabber
  labels:
    app: grabber-service
    version: v1.0.0
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: grabber-service
  template:
    metadata:
      labels:
        app: grabber-service
        version: v1.0.0
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "8080"
        prometheus.io/path: "/metrics"
    spec:
      nodeSelector:
        node-type: gpu-enabled
      tolerations:
      - key: nvidia.com/gpu
        operator: Exists
        effect: NoSchedule
      containers:
      - name: grabber-service
        image: vms/grabber-service:v1.0.0
        imagePullPolicy: IfNotPresent
        ports:
        - name: http
          containerPort: 80
          protocol: TCP
        - name: https
          containerPort: 443
          protocol: TCP
        - name: metrics
          containerPort: 8080
          protocol: TCP
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: INSTANCE_ID
          valueFrom:
            fieldRef:
              fieldPath: metadata.name
        - name: NODE_NAME
          valueFrom:
            fieldRef:
              fieldPath: spec.nodeName
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: grabber-secrets
              key: DB_PASSWORD
        - name: RABBITMQ_PASSWORD
          valueFrom:
            secretKeyRef:
              name: grabber-secrets
              key: RABBITMQ_PASSWORD
        - name: API_KEY
          valueFrom:
            secretKeyRef:
              name: grabber-secrets
              key: API_KEY
        resources:
          requests:
            memory: "4Gi"
            cpu: "1000m"
            nvidia.com/gpu: 1
          limits:
            memory: "16Gi"
            cpu: "4000m"
            nvidia.com/gpu: 1
        volumeMounts:
        - name: config-volume
          mountPath: /app/appsettings.json
          subPath: appsettings.json
        - name: models-volume
          mountPath: /app/models
        - name: tmp-volume
          mountPath: /tmp
        livenessProbe:
          httpGet:
            path: /health/live
            port: http
          initialDelaySeconds: 30
          periodSeconds: 30
          timeoutSeconds: 10
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: http
          initialDelaySeconds: 15
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        startupProbe:
          httpGet:
            path: /health/startup
            port: http
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 5
          failureThreshold: 12
      volumes:
      - name: config-volume
        configMap:
          name: grabber-config
      - name: models-volume
        persistentVolumeClaim:
          claimName: grabber-models-pvc
      - name: tmp-volume
        emptyDir:
          sizeLimit: 1Gi
      securityContext:
        runAsNonRoot: true
        runAsUser: 1001
        fsGroup: 1001
---
# k8s/service.yaml
apiVersion: v1
kind: Service
metadata:
  name: grabber-service
  namespace: vms-grabber
  labels:
    app: grabber-service
spec:
  type: ClusterIP
  ports:
  - name: http
    port: 80
    targetPort: http
    protocol: TCP
  - name: https
    port: 443
    targetPort: https
    protocol: TCP
  selector:
    app: grabber-service
---
# k8s/hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: grabber-service-hpa
  namespace: vms-grabber
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: grabber-service
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
  behavior:
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
      - type: Percent
        value: 50
        periodSeconds: 60
    scaleUp:
      stabilizationWindowSeconds: 60
      policies:
      - type: Percent
        value: 100
        periodSeconds: 30
```

**Deliverables**:
- [x] Complete Kubernetes deployment manifests
- [x] Horizontal Pod Autoscaler configuration
- [x] Persistent storage for models and data
- [x] GPU node affinity and resource allocation

#### Task 4.1.2: Monitoring & Observability Setup
**Duration**: 3 days  
**Assignee**: DevOps Engineer  
**Dependencies**: Task 4.1.1  

**Prometheus & Grafana Configuration**:
```yaml
# monitoring/prometheus-config.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-config
  namespace: monitoring
data:
  prometheus.yml: |
    global:
      scrape_interval: 15s
      evaluation_interval: 15s
    
    rule_files:
      - "/etc/prometheus/rules/*.yml"
    
    alerting:
      alertmanagers:
        - static_configs:
            - targets:
              - alertmanager:9093
    
    scrape_configs:
    - job_name: 'grabber-service'
      kubernetes_sd_configs:
        - role: endpoints
      relabel_configs:
        - source_labels: [__meta_kubernetes_service_annotation_prometheus_io_scrape]
          action: keep
          regex: true
        - source_labels: [__meta_kubernetes_service_annotation_prometheus_io_path]
          action: replace
          target_label: __metrics_path__
          regex: (.+)
        - source_labels: [__address__, __meta_kubernetes_service_annotation_prometheus_io_port]
          action: replace
          regex: ([^:]+)(?::\d+)?;(\d+)
          replacement: $1:$2
          target_label: __address__
        - action: labelmap
          regex: __meta_kubernetes_service_label_(.+)
        - source_labels: [__meta_kubernetes_namespace]
          action: replace
          target_label: kubernetes_namespace
        - source_labels: [__meta_kubernetes_service_name]
          action: replace
          target_label: kubernetes_name
    
    - job_name: 'nvidia-gpu-metrics'
      static_configs:
        - targets: ['gpu-metrics-exporter:9400']
      scrape_interval: 10s
---
# monitoring/alerting-rules.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-rules
  namespace: monitoring
data:
  grabber.yml: |
    groups:
    - name: grabber-service
      rules:
      - alert: GrabberServiceDown
        expr: up{job="grabber-service"} == 0
        for: 30s
        labels:
          severity: critical
        annotations:
          summary: "Grabber service instance is down"
          description: "Grabber service instance {{ $labels.instance }} has been down for more than 30 seconds"
      
      - alert: HighGPUMemoryUsage
        expr: grabber_gpu_memory_utilization_percent > 90
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High GPU memory usage"
          description: "GPU {{ $labels.gpu_id }} memory usage is {{ $value }}% for more than 5 minutes"
      
      - alert: CameraSessionFailures
        expr: rate(grabber_camera_session_failures_total[5m]) > 0.1
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "High camera session failure rate"
          description: "Camera session failure rate is {{ $value }} failures per second"
      
      - alert: FrameProcessingLatencyHigh
        expr: histogram_quantile(0.95, rate(grabber_frame_processing_duration_seconds_bucket[5m])) > 0.5
        for: 3m
        labels:
          severity: warning
        annotations:
          summary: "Frame processing latency high"
          description: "95th percentile frame processing latency is {{ $value }}s"
      
      - alert: FaceRecognitionErrors
        expr: rate(grabber_face_recognition_errors_total[5m]) > 0.05
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "High face recognition error rate"
          description: "Face recognition error rate is {{ $value }} errors per second"
---
# monitoring/grafana-dashboard.json
{
  "dashboard": {
    "id": null,
    "title": "VMS Grabber Service",
    "tags": ["vms", "grabber", "cameras"],
    "timezone": "browser",
    "panels": [
      {
        "title": "Active Camera Sessions",
        "type": "stat",
        "targets": [
          {
            "expr": "grabber_active_camera_sessions",
            "legendFormat": "Active Sessions"
          }
        ],
        "gridPos": {"h": 8, "w": 12, "x": 0, "y": 0}
      },
      {
        "title": "GPU Utilization",
        "type": "gauge",
        "targets": [
          {
            "expr": "grabber_gpu_utilization_percent",
            "legendFormat": "GPU {{gpu_id}}"
          }
        ],
        "fieldConfig": {
          "defaults": {
            "min": 0,
            "max": 100,
            "unit": "percent"
          }
        },
        "gridPos": {"h": 8, "w": 12, "x": 12, "y": 0}
      },
      {
        "title": "Frame Processing Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(grabber_processed_frames_total[5m])",
            "legendFormat": "{{camera_id}} - {{result}}"
          }
        ],
        "yAxes": [
          {"label": "Frames/sec", "min": 0}
        ],
        "gridPos": {"h": 8, "w": 24, "x": 0, "y": 8}
      },
      {
        "title": "Face Detection Results",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(grabber_faces_detected_total[5m])",
            "legendFormat": "Camera {{camera_id}}"
          }
        ],
        "yAxes": [
          {"label": "Detections/sec", "min": 0}
        ],
        "gridPos": {"h": 8, "w": 12, "x": 0, "y": 16}
      },
      {
        "title": "Processing Latency",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.50, rate(grabber_frame_processing_duration_seconds_bucket[5m]))",
            "legendFormat": "50th percentile"
          },
          {
            "expr": "histogram_quantile(0.95, rate(grabber_frame_processing_duration_seconds_bucket[5m]))",
            "legendFormat": "95th percentile"
          }
        ],
        "yAxes": [
          {"label": "Seconds", "min": 0}
        ],
        "gridPos": {"h": 8, "w": 12, "x": 12, "y": 16}
      }
    ],
    "time": {"from": "now-1h", "to": "now"},
    "refresh": "5s"
  }
}
```

**Deliverables**:
- [x] Prometheus metrics collection configuration
- [x] Grafana dashboards for monitoring
- [x] Alerting rules and notifications
- [x] Log aggregation with ELK stack

### 4.2 Operations & Maintenance

#### Task 4.2.1: Automated Operations
**Duration**: 3 days  
**Assignee**: DevOps Engineer + Mid-level Developer  
**Dependencies**: Task 4.1.2  

**Operational Scripts and Automation**:
```bash
#!/bin/bash
# scripts/health-check.sh
# Comprehensive health check script for production operations

set -euo pipefail

NAMESPACE="vms-grabber"
SERVICE_NAME="grabber-service"
PROMETHEUS_URL="http://prometheus:9090"
ALERTMANAGER_URL="http://alertmanager:9093"

echo "🔍 Starting VMS Grabber Service Health Check..."

# Check Kubernetes deployment
echo "Checking Kubernetes deployment status..."
READY_REPLICAS=$(kubectl get deployment ${SERVICE_NAME} -n ${NAMESPACE} -o jsonpath='{.status.readyReplicas}')
DESIRED_REPLICAS=$(kubectl get deployment ${SERVICE_NAME} -n ${NAMESPACE} -o jsonpath='{.spec.replicas}')

if [ "$READY_REPLICAS" != "$DESIRED_REPLICAS" ]; then
    echo "❌ Deployment not healthy: $READY_REPLICAS/$DESIRED_REPLICAS ready"
    kubectl describe deployment ${SERVICE_NAME} -n ${NAMESPACE}
    exit 1
else
    echo "✅ Deployment healthy: $READY_REPLICAS/$DESIRED_REPLICAS ready"
fi

# Check pod health
echo "Checking individual pod health..."
UNHEALTHY_PODS=$(kubectl get pods -n ${NAMESPACE} -l app=${SERVICE_NAME} --field-selector=status.phase!=Running -o name | wc -l)
if [ "$UNHEALTHY_PODS" -gt 0 ]; then
    echo "❌ Found $UNHEALTHY_PODS unhealthy pods"
    kubectl get pods -n ${NAMESPACE} -l app=${SERVICE_NAME} --field-selector=status.phase!=Running
    exit 1
else
    echo "✅ All pods healthy"
fi

# Check service endpoints
echo "Checking service endpoints..."
ENDPOINTS=$(kubectl get endpoints ${SERVICE_NAME} -n ${NAMESPACE} -o jsonpath='{.subsets[0].addresses[*].ip}' | wc -w)
if [ "$ENDPOINTS" -lt 1 ]; then
    echo "❌ No healthy endpoints found"
    kubectl describe endpoints ${SERVICE_NAME} -n ${NAMESPACE}
    exit 1
else
    echo "✅ Found $ENDPOINTS healthy endpoints"
fi

# Check GPU resources
echo "Checking GPU resource allocation..."
GPU_PODS=$(kubectl get pods -n ${NAMESPACE} -l app=${SERVICE_NAME} -o jsonpath='{.items[*].spec.containers[*].resources.requests.nvidia\.com/gpu}' | tr ' ' '\n' | grep -v '^$' | wc -l)
if [ "$GPU_PODS" -lt 1 ]; then
    echo "⚠️  Warning: No GPU resources allocated"
else
    echo "✅ GPU resources allocated to $GPU_PODS pods"
fi

# Check metrics availability
echo "Checking Prometheus metrics..."
if curl -s "${PROMETHEUS_URL}/api/v1/query?query=up{job=\"grabber-service\"}" | jq -r '.data.result[] | select(.value[1] == "1")' | wc -l > 0; then
    echo "✅ Prometheus metrics available"
else
    echo "❌ Prometheus metrics not available"
    exit 1
fi

# Check for active alerts
echo "Checking for active alerts..."
ACTIVE_ALERTS=$(curl -s "${ALERTMANAGER_URL}/api/v1/alerts" | jq '[.data[] | select(.status.state == "active" and .labels.job == "grabber-service")] | length')
if [ "$ACTIVE_ALERTS" -gt 0 ]; then
    echo "⚠️  Warning: $ACTIVE_ALERTS active alerts found"
    curl -s "${ALERTMANAGER_URL}/api/v1/alerts" | jq '.data[] | select(.status.state == "active" and .labels.job == "grabber-service") | {alertname: .labels.alertname, description: .annotations.description}'
else
    echo "✅ No active alerts"
fi

# Performance check
echo "Checking performance metrics..."
AVG_LATENCY=$(curl -s "${PROMETHEUS_URL}/api/v1/query?query=rate(grabber_frame_processing_duration_seconds_sum[5m])/rate(grabber_frame_processing_duration_seconds_count[5m])" | jq -r '.data.result[0].value[1] // "0"')
if (( $(echo "$AVG_LATENCY > 0.2" | bc -l) )); then
    echo "⚠️  Warning: High average processing latency: ${AVG_LATENCY}s"
else
    echo "✅ Processing latency normal: ${AVG_LATENCY}s"
fi

echo "🎉 Health check completed successfully!"
```

```bash
#!/bin/bash
# scripts/deploy.sh
# Automated deployment script with rollback capabilities

set -euo pipefail

NAMESPACE="vms-grabber"
SERVICE_NAME="grabber-service"
IMAGE_TAG=${1:-latest}
TIMEOUT=${2:-300}

echo "🚀 Starting deployment of ${SERVICE_NAME}:${IMAGE_TAG}..."

# Pre-deployment checks
echo "Running pre-deployment checks..."
./scripts/health-check.sh

# Store current deployment for rollback
echo "Backing up current deployment..."
kubectl get deployment ${SERVICE_NAME} -n ${NAMESPACE} -o yaml > /tmp/deployment-backup-$(date +%Y%m%d-%H%M%S).yaml

# Update deployment
echo "Updating deployment image..."
kubectl set image deployment/${SERVICE_NAME} -n ${NAMESPACE} grabber-service=vms/grabber-service:${IMAGE_TAG}

# Wait for rollout
echo "Waiting for rollout to complete..."
if kubectl rollout status deployment/${SERVICE_NAME} -n ${NAMESPACE} --timeout=${TIMEOUT}s; then
    echo "✅ Deployment successful"
else
    echo "❌ Deployment failed, initiating rollback..."
    kubectl rollout undo deployment/${SERVICE_NAME} -n ${NAMESPACE}
    kubectl rollout status deployment/${SERVICE_NAME} -n ${NAMESPACE} --timeout=180s
    echo "🔄 Rollback completed"
    exit 1
fi

# Post-deployment verification
echo "Running post-deployment verification..."
sleep 30  # Allow time for services to stabilize
./scripts/health-check.sh

# Run smoke tests
echo "Running smoke tests..."
./scripts/smoke-test.sh

echo "🎉 Deployment completed successfully!"
```

```bash
#!/bin/bash
# scripts/backup.sh
# Backup critical data and configurations

set -euo pipefail

NAMESPACE="vms-grabber"
BACKUP_DIR="/backups/$(date +%Y%m%d-%H%M%S)"
RETENTION_DAYS=7

echo "📦 Starting backup process..."

# Create backup directory
mkdir -p ${BACKUP_DIR}

# Backup Kubernetes configurations
echo "Backing up Kubernetes configurations..."
kubectl get all -n ${NAMESPACE} -o yaml > ${BACKUP_DIR}/k8s-resources.yaml
kubectl get secrets -n ${NAMESPACE} -o yaml > ${BACKUP_DIR}/secrets.yaml
kubectl get configmaps -n ${NAMESPACE} -o yaml > ${BACKUP_DIR}/configmaps.yaml

# Backup persistent volumes
echo "Backing up persistent volume data..."
for pvc in $(kubectl get pvc -n ${NAMESPACE} -o jsonpath='{.items[*].metadata.name}'); do
    echo "Creating snapshot of PVC ${pvc}..."
    kubectl create -f - <<EOF
apiVersion: snapshot.storage.k8s.io/v1
kind: VolumeSnapshot
metadata:
  name: ${pvc}-backup-$(date +%Y%m%d-%H%M%S)
  namespace: ${NAMESPACE}
spec:
  source:
    persistentVolumeClaimName: ${pvc}
EOF
done

# Backup database
echo "Backing up database..."
DB_POD=$(kubectl get pods -n ${NAMESPACE} -l app=postgres -o jsonpath='{.items[0].metadata.name}')
kubectl exec -n ${NAMESPACE} ${DB_POD} -- pg_dump -U grabber_user grabber_db > ${BACKUP_DIR}/database-backup.sql

# Export metrics data
echo "Exporting recent metrics data..."
curl -s "http://prometheus:9090/api/v1/export?match[]={job=\"grabber-service\"}&start=$(date -d '1 hour ago' +%s)&end=$(date +%s)" > ${BACKUP_DIR}/metrics-export.json

# Create backup archive
echo "Creating backup archive..."
tar -czf ${BACKUP_DIR}.tar.gz -C $(dirname ${BACKUP_DIR}) $(basename ${BACKUP_DIR})
rm -rf ${BACKUP_DIR}

# Cleanup old backups
echo "Cleaning up old backups..."
find /backups -name "*.tar.gz" -mtime +${RETENTION_DAYS} -delete

echo "✅ Backup completed: ${BACKUP_DIR}.tar.gz"
```

**Deliverables**:
- [x] Automated health checking and monitoring scripts
- [x] Blue-green deployment with rollback capabilities
- [x] Backup and disaster recovery procedures
- [x] Performance optimization and tuning scripts

---

## Risk Management & Mitigation

### Technical Risks

| Risk | Impact | Probability | Mitigation Strategy | Owner |
|------|--------|-------------|-------------------|--------|
| **GPU Driver Compatibility Issues** | High | Medium | • Extensive testing on target hardware<br>• Docker images with verified driver versions<br>• Fallback to CPU processing | Senior Dev |
| **ONNX Model Performance Issues** | High | Low | • Benchmark multiple model architectures<br>• Model optimization and quantization<br>• Progressive deployment with A/B testing | Senior Dev |
| **Memory Leaks in GPU Processing** | High | Medium | • Comprehensive memory profiling<br>• Automated leak detection<br>• Resource pooling and cleanup | Senior Dev |
| **RabbitMQ Message Loss** | Medium | Low | • Persistent queues with durability<br>• Message acknowledgments<br>• Dead letter queue processing | Mid-level Dev |
| **Camera Hardware Incompatibility** | Medium | Medium | • Extensive camera compatibility testing<br>• Generic ONVIF implementation<br>• Manufacturer-specific handlers | Mid-level Dev |

### Operational Risks

| Risk | Impact | Probability | Mitigation Strategy | Owner |
|------|--------|-------------|-------------------|--------|
| **Production Deployment Failures** | High | Low | • Comprehensive staging environment<br>• Blue-green deployment strategy<br>• Automated rollback procedures | DevOps |
| **Scaling Issues Under Load** | Medium | Medium | • Load testing with synthetic data<br>• Horizontal pod autoscaling<br>• Performance monitoring and alerts | DevOps |
| **Security Vulnerabilities** | High | Low | • Security code review and audits<br>• Penetration testing<br>• Regular dependency updates | Senior Dev |
| **Data Privacy Compliance** | Medium | Low | • GDPR/privacy compliance review<br>• Data retention policies<br>• Audit logging | Senior Dev |

### Resource & Timeline Risks

| Risk | Impact | Probability | Mitigation Strategy | Owner |
|------|--------|-------------|-------------------|--------|
| **GPU Hardware Availability** | High | Medium | • Early hardware procurement<br>• Cloud GPU alternatives (AWS/Azure)<br>• Development with GPU simulators | Project Manager |
| **Team Knowledge Gaps** | Medium | Medium | • Early training on GPU programming<br>• External consulting support<br>• Knowledge transfer sessions | Project Manager |
| **Integration Complexity** | Medium | High | • Early integration testing<br>• Incremental delivery approach<br>• Prototype validation | Senior Dev |

## Resource Allocation

### Team Structure

**Core Development Team (4-6 members)**
- **Senior Developer (2)**: GPU processing, facial recognition, architecture
- **Mid-level Developer (2-3)**: Camera handling, message queuing, API development  
- **DevOps Engineer (1)**: Infrastructure, deployment, monitoring
- **Project Manager (1)**: Coordination, risk management, stakeholder communication

### Hardware Requirements

**Development Environment**
- 3x Development machines with NVIDIA RTX 3070/4060 Ti
- 1x Testing server with multiple GPU configuration
- Network attached storage for model and data sharing

**Production Environment**  
- 3-5x GPU-enabled servers (NVIDIA RTX 3070/4070 or better)
- High-speed network infrastructure (10Gb minimum)
- Shared storage for models and backups

### Budget Estimation

**Development Phase (16 weeks)**
- Personnel: $240,000 - $320,000
- Hardware: $50,000 - $75,000  
- Software licenses: $10,000 - $15,000
- Cloud resources: $5,000 - $10,000

**Total Project Cost: $305,000 - $420,000**

## Success Criteria & Acceptance

### Performance Benchmarks
- ✅ **Concurrent Cameras**: Support 50+ simultaneous camera streams
- ✅ **Processing Latency**: <100ms average frame processing time
- ✅ **Face Detection Accuracy**: >95% detection rate with <2% false positives
- ✅ **GPU Utilization**: 80-95% utilization during peak load
- ✅ **System Reliability**: 99.9% uptime with automatic recovery

### Functional Requirements
- ✅ **Camera Support**: RTSP, USB, IP, and ONVIF camera types
- ✅ **Real-time Processing**: Live facial recognition and visitor matching
- ✅ **Scalable Architecture**: Horizontal scaling with load balancing
- ✅ **Integration**: Seamless integration with existing VMS API
- ✅ **Security**: API authentication, rate limiting, data encryption

### Operational Requirements
- ✅ **Monitoring**: Comprehensive metrics and alerting
- ✅ **Deployment**: Automated CI/CD with rollback capabilities  
- ✅ **Recovery**: Disaster recovery and backup procedures
- ✅ **Documentation**: Complete technical and operational documentation

---

## Timeline Summary

| Phase | Duration | Key Deliverables | Critical Path |
|-------|----------|-----------------|---------------|
| **Phase 1**: Foundation | 4 weeks | Infrastructure, messaging, domain models | ✅ Critical |
| **Phase 2**: Core Engine | 4 weeks | Camera handling, GPU management, face recognition | ✅ Critical |
| **Phase 3**: Production Ready | 4 weeks | Monitoring, error handling, security | ⚠️ Parallel |
| **Phase 4**: Deployment | 4 weeks | Production deployment, operations, optimization | ⚠️ Parallel |

**Total Timeline: 16 weeks**  
**Critical Path: 12 weeks** (Phases 1-2 sequential, Phases 3-4 parallel)

## Conclusion

This comprehensive implementation plan provides a structured approach to building a production-ready Camera Frame Grabber Service. The phased approach ensures solid foundations are built before adding complexity, while the parallel execution of later phases optimizes delivery time.

Key success factors:
1. **Early GPU hardware acquisition** to prevent delays
2. **Incremental integration testing** to catch issues early
3. **Comprehensive monitoring** from day one of development
4. **Security-first approach** with built-in protection mechanisms
5. **Scalable architecture** designed for growth and performance

The plan balances technical excellence with practical delivery constraints, providing multiple risk mitigation strategies and clear success criteria for each phase.

---

*Document Version: 1.0*  
*Last Updated: January 2025*  
*Total Pages: Planning Roadmap Complete*
