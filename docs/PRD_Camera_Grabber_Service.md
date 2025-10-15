# Product Requirements Document: Camera Frame Grabber Service

## Executive Summary

The Camera Frame Grabber Service is a dedicated microservice designed to handle intensive camera operations including frame capture, facial recognition processing, and GPU-accelerated video analysis. This service will operate independently from the main VMS API, communicating via RabbitMQ message queues and providing real-time updates through SignalR integration.

## 1. Business Context & Objectives

### Current State Analysis
- VMS API contains placeholder camera operations with no actual hardware integration
- Camera management is fully implemented (CRUD, configuration, status tracking)
- MediatR pattern established for commands/queries
- SignalR hubs ready for real-time communication
- Background service patterns already in use

### Business Objectives
- **Scalability**: Handle multiple cameras simultaneously with GPU acceleration
- **Performance**: Dedicated service prevents camera operations from blocking main API
- **Reliability**: Isolated failures don't affect core VMS functionality
- **Resource Optimization**: Efficient GPU utilization for facial recognition
- **Maintainability**: Specialized service for camera-specific operations

### Success Metrics
- Support for 50+ concurrent camera streams
- <100ms average frame processing latency
- 99.9% service availability
- GPU utilization >80% during peak loads
- Zero impact on main API performance during camera operations

## 2. System Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            VMS Ecosystem                                    │
├─────────────────────────────┬───────────────────────────────────────────────┤
│        VMS API              │           Grabber Service                      │
│                             │                                               │
│  ┌─────────────────┐       │    ┌─────────────────────────────────────┐    │
│  │ CamerasController│       │    │         Grabber API                 │    │
│  │                 │       │    │  ┌─────────────────────────────────┐│    │
│  │ - CRUD          │       │    │  │ Camera Management Controller    ││    │
│  │ - Configuration │◄──────┼────┤  │ Health Check Controller         ││    │
│  │ - Status        │       │    │  │ Frame Processing Controller     ││    │
│  └─────────────────┘       │    │  └─────────────────────────────────┘│    │
│                             │    │                                     │    │
│  ┌─────────────────┐       │    │  ┌─────────────────────────────────┐│    │
│  │ IGrabberClient  │       │    │  │      Frame Capture Engine       ││    │
│  │ Service         │◄──────┼────┤  │  ┌─────────────────────────────┐││    │
│  │                 │       │    │  │  │ RTSP Handler                │││    │
│  │ - Send Commands │       │    │  │  │ USB Handler                 │││    │
│  │ - Process Events│       │    │  │  │ IP Camera Handler           │││    │
│  └─────────────────┘       │    │  │  │ ONVIF Handler               │││    │
│                             │    │  │  └─────────────────────────────┘││    │
│           │                 │    │  └─────────────────────────────────┘│    │
│           │                 │    │                                     │    │
│           ▼                 │    │  ┌─────────────────────────────────┐│    │
│  ┌─────────────────┐       │    │  │   Facial Recognition Engine     ││    │
│  │ SignalR Hubs    │       │    │  │  ┌─────────────────────────────┐││    │
│  │                 │       │    │  │  │ GPU Processing              │││    │
│  │ - SecurityHub   │       │    │  │  │ Face Detection              │││    │
│  │ - AdminHub      │       │    │  │  │ Face Recognition            │││    │
│  │ - OperatorHub   │       │    │  │  │ Visitor Matching            │││    │
│  └─────────────────┘       │    │  │  └─────────────────────────────┘││    │
│                             │    │  └─────────────────────────────────┘│    │
└─────────────────────────────┤    └─────────────────────────────────────┘    │
                              │                          │                    │
                              │                          │                    │
                    ┌─────────▼──────────┐              │                    │
                    │    RabbitMQ         │              │                    │
                    │                     │              │                    │
                    │ ┌─────────────────┐ │              │                    │
                    │ │Camera Commands  │ │◄─────────────┘                    │
                    │ │Queue            │ │                                   │
                    │ └─────────────────┘ │                                   │
                    │                     │                                   │
                    │ ┌─────────────────┐ │                                   │
                    │ │FR Results       │ │                                   │
                    │ │Queue            │ │                                   │
                    │ └─────────────────┘ │                                   │
                    │                     │                                   │
                    │ ┌─────────────────┐ │                                   │
                    │ │Health Events    │ │                                   │
                    │ │Queue            │ │                                   │
                    │ └─────────────────┘ │                                   │
                    └─────────────────────┘                                   │
                                                                              │
                    ┌─────────────────────────────────────────────────────────┘
                    │
                    ▼
              ┌─────────────────┐
              │   Database      │
              │                 │
              │ Camera Configs  │
              │ Processing Logs │
              │ Performance     │
              │ Metrics        │
              └─────────────────┘
```

### Service Components

#### VMS API Components
1. **CamerasController** (Enhanced)
2. **IGrabberClientService** (New)
3. **SignalR Hubs** (Enhanced)
4. **RabbitMQ Publishers** (New)

#### Grabber Service Components
1. **Camera Management API**
2. **Frame Capture Engine**
3. **Facial Recognition Engine** 
4. **RabbitMQ Consumers**
5. **SignalR Client**
6. **Health Monitoring System**

## 3. Message Flow Architecture

### 3.1 Camera Command Flow

```
VMS API → RabbitMQ → Grabber Service → Processing → Results → RabbitMQ → VMS API → SignalR
```

**Detailed Flow:**
1. **User Action** → VMS CamerasController
2. **Command Creation** → MediatR Command Handler
3. **Message Publishing** → RabbitMQ Camera Commands Queue
4. **Message Consumption** → Grabber Service Worker
5. **Camera Operation** → Frame Capture Engine
6. **Result Publishing** → RabbitMQ Results Queue  
7. **Result Processing** → VMS Background Service
8. **Real-time Update** → SignalR Hub → Frontend

### 3.2 Message Types & Queues

#### Camera Commands Queue
```json
{
  "messageType": "StartCameraStream",
  "cameraId": 123,
  "requestId": "req_abc123",
  "timestamp": "2025-01-15T10:30:00Z",
  "payload": {
    "enableFacialRecognition": true,
    "streamConfiguration": {
      "resolution": "1920x1080",
      "frameRate": 30,
      "quality": 75
    }
  }
}
```

#### Face Recognition Results Queue
```json
{
  "messageType": "FaceDetected",
  "cameraId": 123,
  "requestId": "req_abc123", 
  "timestamp": "2025-01-15T10:30:15Z",
  "payload": {
    "detections": [
      {
        "boundingBox": {"x": 100, "y": 150, "width": 80, "height": 100},
        "confidence": 0.95,
        "visitorId": 456,
        "matchConfidence": 0.87,
        "frameTimestamp": "2025-01-15T10:30:14.500Z"
      }
    ],
    "frameId": "frame_xyz789",
    "processingTimeMs": 45
  }
}
```

#### Health Events Queue
```json
{
  "messageType": "CameraHealthUpdate",
  "cameraId": 123,
  "timestamp": "2025-01-15T10:30:00Z",
  "payload": {
    "status": "Active",
    "previousStatus": "Connecting",
    "responseTimeMs": 120,
    "errorMessage": null,
    "isRecovery": true,
    "healthScore": 95.0
  }
}
```

## 4. Technical Specifications

### 4.1 Grabber Service Technology Stack

#### Core Framework
- **.NET 8**: Latest performance optimizations
- **ASP.NET Core**: Web API and dependency injection
- **Entity Framework Core**: Database operations
- **MediatR**: CQRS pattern consistency

#### Message Queuing
- **RabbitMQ**: Message broker with clustering support
- **MassTransit**: .NET abstraction for RabbitMQ
- **Dead Letter Queues**: Failed message handling
- **Message Persistence**: Durability guarantees

#### Camera & Processing
- **FFmpeg**: Video processing and codec support
- **OpenCV**: Computer vision operations
- **CUDA/cuDNN**: GPU acceleration (NVIDIA)
- **ONNXRuntime**: Machine learning inference
- **Media Foundation**: Windows camera APIs

#### Monitoring & Observability
- **Serilog**: Structured logging
- **Prometheus**: Metrics collection
- **Health Checks**: Service monitoring
- **Performance Counters**: System metrics

### 4.2 Hardware Requirements

#### Minimum Configuration
- **CPU**: Intel i5-8400 / AMD Ryzen 5 2600
- **RAM**: 16GB DDR4
- **GPU**: NVIDIA GTX 1060 6GB / RTX 3060
- **Storage**: 500GB SSD
- **Network**: Gigabit Ethernet

#### Recommended Configuration
- **CPU**: Intel i7-10700K / AMD Ryzen 7 3700X
- **RAM**: 32GB DDR4 3200MHz
- **GPU**: NVIDIA RTX 3070 / RTX 4060 Ti
- **Storage**: 1TB NVMe SSD
- **Network**: 10Gb Ethernet (for high camera counts)

### 4.3 Performance Specifications

#### Scalability Targets
- **Concurrent Cameras**: 50+ streams
- **Frame Processing**: 1000+ FPS total throughput
- **Facial Recognition**: 100+ faces/second
- **GPU Utilization**: 80-95% during peak loads
- **Memory Usage**: <8GB per 25 cameras

#### Latency Requirements
- **Frame Capture**: <50ms
- **Face Detection**: <100ms per frame
- **Face Recognition**: <200ms per face
- **End-to-End**: <500ms camera to UI update

## 5. Service Scaling & GPU Management

### 5.1 Multi-Instance Architecture

#### Horizontal Scaling Pattern
```
                    ┌─────────────────────────────────┐
                    │         Load Balancer            │
                    │    (Camera Assignment)           │
                    └─────────────┬───────────────────┘
                                  │
        ┌─────────────────────────┼─────────────────────────┐
        │                         │                         │
        ▼                         ▼                         ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Grabber #1     │    │  Grabber #2     │    │  Grabber #3     │
│                 │    │                 │    │                 │
│  GPU: RTX 3070  │    │  GPU: RTX 3070  │    │  GPU: RTX 3070  │
│  Cameras: 1-16  │    │  Cameras: 17-32 │    │  Cameras: 33-48 │
│  RAM: 10GB      │    │  RAM: 10GB      │    │  RAM: 10GB      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
        │                         │                         │
        └─────────────────────────┼─────────────────────────┘
                                  │
                                  ▼
                    ┌─────────────────────────────────┐
                    │          RabbitMQ               │
                    │      Message Queues             │
                    └─────────────────────────────────┘
```

#### Camera Assignment Strategy
1. **Static Assignment**: Cameras assigned to specific instances
2. **Dynamic Load Balancing**: Assignment based on current load
3. **Failover Support**: Automatic reassignment on instance failure
4. **GPU Affinity**: Cameras grouped by GPU capabilities

### 5.2 GPU Resource Management

#### GPU Memory Allocation
```csharp
public class GpuResourceManager
{
    private readonly Dictionary<int, GpuAllocation> _allocations = new();
    private readonly SemaphoreSlim _gpuSemaphore;
    private readonly int _maxConcurrentProcessing;
    
    public async Task<GpuAllocation> AllocateAsync(int cameraId, 
        GpuResourceRequirements requirements)
    {
        await _gpuSemaphore.WaitAsync();
        try
        {
            var allocation = new GpuAllocation
            {
                CameraId = cameraId,
                MemoryMB = requirements.EstimatedMemoryMB,
                ComputeUnits = requirements.ComputeUnits,
                AllocatedAt = DateTime.UtcNow
            };
            
            _allocations[cameraId] = allocation;
            return allocation;
        }
        finally
        {
            _gpuSemaphore.Release();
        }
    }
}
```

#### Processing Pipeline Optimization
1. **Batch Processing**: Multiple frames processed together
2. **Memory Pooling**: Reuse GPU memory allocations  
3. **Pipeline Parallelism**: Overlap CPU/GPU operations
4. **Model Caching**: Keep ML models loaded in GPU memory

## 6. Integration Specifications

### 6.1 VMS API Integration

#### IGrabberClientService Interface
```csharp
public interface IGrabberClientService
{
    // Camera Control
    Task<bool> StartCameraAsync(int cameraId, CameraStartOptions options);
    Task<bool> StopCameraAsync(int cameraId, bool graceful = true);
    Task<bool> RestartCameraAsync(int cameraId);
    
    // Health Monitoring  
    Task<CameraHealthStatus> GetCameraHealthAsync(int cameraId);
    Task<IEnumerable<CameraHealthStatus>> GetAllCameraHealthAsync();
    
    // Configuration
    Task<bool> UpdateCameraConfigAsync(int cameraId, CameraConfiguration config);
    Task<bool> TestCameraConnectionAsync(int cameraId);
    
    // Frame Operations
    Task<byte[]> CaptureFrameAsync(int cameraId);
    Task<bool> EnableFacialRecognitionAsync(int cameraId, bool enabled);
    
    // Service Management
    Task<GrabberServiceStatus> GetServiceStatusAsync();
    Task<GrabberServiceMetrics> GetServiceMetricsAsync();
}
```

#### Enhanced CamerasController Integration
```csharp
[HttpPost("{id:int}/start-stream")]
public async Task<IActionResult> StartStream(int id)
{
    var camera = await _mediator.Send(new GetCameraByIdQuery { Id = id });
    if (camera == null) return NotFoundResponse("Camera", id);
    
    var options = new CameraStartOptions
    {
        EnableFacialRecognition = camera.EnableFacialRecognition,
        Configuration = camera.GetConfiguration(),
        Priority = camera.Priority
    };
    
    var result = await _grabberClient.StartCameraAsync(id, options);
    
    if (result)
    {
        await _mediator.Send(new UpdateCameraStatusCommand 
        { 
            Id = id, 
            Status = CameraStatus.Connecting 
        });
    }
    
    return SuccessResponse(new { Success = result, StartedAt = DateTime.UtcNow });
}
```

### 6.2 SignalR Integration

#### Real-time Event Broadcasting
```csharp
public class GrabberEventProcessor : IConsumer<FaceDetectionResult>
{
    private readonly IHubContext<SecurityHub> _securityHub;
    private readonly IMediator _mediator;
    
    public async Task Consume(ConsumeContext<FaceDetectionResult> context)
    {
        var result = context.Message;
        
        // Update database
        await _mediator.Send(new RecordFaceDetectionCommand
        {
            CameraId = result.CameraId,
            VisitorId = result.VisitorId,
            Confidence = result.Confidence,
            DetectedAt = result.Timestamp
        });
        
        // Broadcast to connected clients
        await _securityHub.Clients.All.SendAsync("FaceDetected", new
        {
            CameraId = result.CameraId,
            CameraName = result.CameraName,
            VisitorId = result.VisitorId,
            VisitorName = result.VisitorName,
            Confidence = result.Confidence,
            Location = result.Location,
            DetectedAt = result.Timestamp
        });
    }
}
```

## 7. Error Handling & Recovery

### 7.1 Failure Scenarios & Recovery

#### Camera Connection Failures
```csharp
public class CameraConnectionRecovery
{
    private readonly ExponentialBackoff _backoff = new(TimeSpan.FromSeconds(1), 
        TimeSpan.FromMinutes(5), 10);
    private readonly ILogger<CameraConnectionRecovery> _logger;
    private readonly IRabbitMQPublisher _publisher;
    
    public async Task<bool> AttemptRecoveryAsync(int cameraId, 
        Exception lastException)
    {
        var camera = await GetCameraAsync(cameraId);
        
        for (int attempt = 1; attempt <= _backoff.MaxRetries; attempt++)
        {
            await Task.Delay(_backoff.GetDelay(attempt));
            
            try
            {
                var connected = await TestConnectionAsync(camera);
                if (connected)
                {
                    await _publisher.PublishAsync(new CameraHealthEvent
                    {
                        CameraId = cameraId,
                        Status = CameraStatus.Active,
                        IsRecovery = true,
                        RecoveryAttempt = attempt,
                        PreviousError = lastException.Message
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Recovery attempt {Attempt} failed for camera {CameraId}", 
                    attempt, cameraId);
            }
        }
        
        await _publisher.PublishAsync(new CameraHealthEvent
        {
            CameraId = cameraId,
            Status = CameraStatus.Error,
            ErrorMessage = "Recovery failed after maximum attempts",
            FailureCount = _backoff.MaxRetries
        });
        return false;
    }
}
```

#### GPU Resource Exhaustion Handling
```csharp
public class GpuResourceMonitor : BackgroundService
{
    private readonly IGpuMetrics _gpuMetrics;
    private readonly ILogger<GpuResourceMonitor> _logger;
    private readonly Timer _monitoringTimer;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var gpuStatus = await _gpuMetrics.GetCurrentStatusAsync();
                
                if (gpuStatus.MemoryUsagePercent > 90)
                {
                    _logger.LogWarning("GPU memory usage critical: {Usage}%", 
                        gpuStatus.MemoryUsagePercent);
                    
                    await TriggerGarbageCollection();
                    await PauseNonCriticalProcessing();
                }
                else if (gpuStatus.MemoryUsagePercent > 75)
                {
                    await ReduceProcessingQuality();
                }
                
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GPU monitoring failed");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
    
    private async Task TriggerGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        await _gpuMetrics.ClearUnusedMemoryAsync();
    }
}
```

### 7.2 Message Queue Resilience

#### Dead Letter Queue Processing
```csharp
public class DeadLetterQueueProcessor : BackgroundService
{
    private readonly IConsumer<object> _deadLetterConsumer;
    private readonly ILogger<DeadLetterQueueProcessor> _logger;
    private readonly INotificationService _notifications;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _deadLetterConsumer.ConsumeAsync(async (message, context) =>
        {
            var retryCount = context.Headers.GetValueOrDefault("retry-count", "0");
            var originalQueue = context.Headers.GetValueOrDefault("original-queue", "unknown");
            
            _logger.LogError("Processing dead letter message from {Queue}, retry count: {Count}", 
                originalQueue, retryCount);
            
            // Attempt to determine root cause
            var errorAnalysis = await AnalyzeFailureReason(message, context);
            
            // Send alert to operations team
            await _notifications.SendAlertAsync(new DeadLetterAlert
            {
                OriginalQueue = originalQueue,
                RetryCount = int.Parse(retryCount),
                ErrorAnalysis = errorAnalysis,
                MessageContent = message.ToString(),
                Timestamp = DateTime.UtcNow
            });
            
            // Attempt manual intervention if possible
            if (errorAnalysis.IsRecoverable)
            {
                await AttemptMessageRecovery(message, context);
            }
            
        }, stoppingToken);
    }
}
```

## 8. Security Implementation

### 8.1 Camera Credential Encryption
```csharp
public class CameraCredentialManager
{
    private readonly IDataProtectionProvider _dataProtection;
    private readonly IKeyVault _keyVault;
    private readonly ILogger<CameraCredentialManager> _logger;
    
    public async Task<string> EncryptCredentialsAsync(string password, int cameraId)
    {
        try
        {
            // Use camera-specific encryption key
            var protector = _dataProtection.CreateProtector($"camera-{cameraId}");
            var encrypted = protector.Protect(password);
            
            // Store encryption metadata in key vault
            await _keyVault.SetSecretAsync($"camera-{cameraId}-meta", new
            {
                EncryptedAt = DateTime.UtcNow,
                KeyVersion = "v1",
                Algorithm = "AES-256-GCM"
            });
            
            return encrypted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt credentials for camera {CameraId}", cameraId);
            throw;
        }
    }
    
    public async Task<string> DecryptCredentialsAsync(string encryptedPassword, int cameraId)
    {
        try
        {
            var protector = _dataProtection.CreateProtector($"camera-{cameraId}");
            return protector.Unprotect(encryptedPassword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt credentials for camera {CameraId}", cameraId);
            throw new SecurityException("Credential decryption failed", ex);
        }
    }
}
```

### 8.2 Network Security Configuration
```csharp
public class SecureNetworkConfiguration
{
    public static void ConfigureNetworkSecurity(IServiceCollection services, 
        IConfiguration configuration)
    {
        // Configure HTTPS with strong cipher suites
        services.Configure<KestrelServerOptions>(options =>
        {
            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                httpsOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                httpsOptions.CheckCertificateRevocation = true;
            });
        });
        
        // Configure mutual TLS for camera communications
        services.Configure<HttpClientFactoryOptions>("camera-client", options =>
        {
            options.HttpClientActions.Add(client =>
            {
                var handler = new HttpClientHandler()
                {
                    ServerCertificateCustomValidationCallback = ValidateCameraCertificate,
                    ClientCertificateOptions = ClientCertificateOption.Manual
                };
                
                // Load client certificate for mutual TLS
                var clientCert = LoadClientCertificate(configuration);
                handler.ClientCertificates.Add(clientCert);
            });
        });
    }
    
    private static bool ValidateCameraCertificate(HttpRequestMessage request, 
        X509Certificate2 certificate, X509Chain chain, SslPolicyErrors errors)
    {
        // Implement custom certificate validation logic
        return CertificateValidator.ValidateCameraCertificate(certificate, chain, errors);
    }
}
```

## 9. Performance Optimization

### 9.1 GPU Memory Pool Implementation
```csharp
public class GpuMemoryPool : IDisposable
{
    private readonly ConcurrentQueue<IntPtr> _availableBuffers = new();
    private readonly Dictionary<IntPtr, GpuBufferInfo> _allocatedBuffers = new();
    private readonly SemaphoreSlim _allocationSemaphore;
    private readonly int _bufferSize;
    private readonly int _maxBuffers;
    private volatile bool _disposed;
    
    public GpuMemoryPool(int bufferSizeBytes, int maxBuffers = 50)
    {
        _bufferSize = bufferSizeBytes;
        _maxBuffers = maxBuffers;
        _allocationSemaphore = new SemaphoreSlim(maxBuffers, maxBuffers);
        
        // Pre-allocate GPU buffers
        for (int i = 0; i < maxBuffers; i++)
        {
            var buffer = AllocateGpuMemory(bufferSizeBytes);
            _availableBuffers.Enqueue(buffer);
        }
    }
    
    public async Task<GpuBuffer> RentAsync(CancellationToken cancellationToken = default)
    {
        await _allocationSemaphore.WaitAsync(cancellationToken);
        
        if (_availableBuffers.TryDequeue(out var buffer))
        {
            var bufferInfo = new GpuBufferInfo
            {
                Buffer = buffer,
                AllocatedAt = DateTime.UtcNow,
                Size = _bufferSize
            };
            
            _allocatedBuffers[buffer] = bufferInfo;
            return new GpuBuffer(buffer, bufferInfo.Size, this);
        }
        
        throw new InvalidOperationException("No GPU buffers available");
    }
    
    public void Return(IntPtr buffer)
    {
        if (_allocatedBuffers.TryRemove(buffer, out var info))
        {
            // Clear buffer before returning to pool
            ClearGpuMemory(buffer, info.Size);
            _availableBuffers.Enqueue(buffer);
            _allocationSemaphore.Release();
        }
    }
    
    private IntPtr AllocateGpuMemory(int size)
    {
        // Use CUDA memory allocation
        var result = CudaApi.cudaMalloc(out IntPtr devPtr, (uint)size);
        if (result != CudaError.Success)
        {
            throw new OutOfMemoryException($"CUDA memory allocation failed: {result}");
        }
        return devPtr;
    }
}
```

### 9.2 Batch Processing Engine
```csharp
public class BatchFrameProcessor
{
    private readonly Channel<FrameProcessingRequest> _frameChannel;
    private readonly GpuMemoryPool _memoryPool;
    private readonly IFacialRecognitionEngine _recognitionEngine;
    private readonly int _batchSize;
    private readonly int _maxWaitTimeMs;
    
    public BatchFrameProcessor(int batchSize = 8, int maxWaitTimeMs = 100)
    {
        _batchSize = batchSize;
        _maxWaitTimeMs = maxWaitTimeMs;
        
        var channelOptions = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        
        _frameChannel = Channel.CreateBounded<FrameProcessingRequest>(channelOptions);
        
        // Start batch processing task
        _ = Task.Run(ProcessFrameBatches);
    }
    
    private async Task ProcessFrameBatches()
    {
        var batch = new List<FrameProcessingRequest>(_batchSize);
        var reader = _frameChannel.Reader;
        
        while (await reader.WaitToReadAsync())
        {
            var batchStartTime = DateTime.UtcNow;
            
            // Collect batch items with timeout
            while (batch.Count < _batchSize && 
                   reader.TryRead(out var request) && 
                   (DateTime.UtcNow - batchStartTime).TotalMilliseconds < _maxWaitTimeMs)
            {
                batch.Add(request);
            }
            
            if (batch.Count > 0)
            {
                await ProcessBatch(batch);
                batch.Clear();
            }
        }
    }
    
    private async Task ProcessBatch(List<FrameProcessingRequest> batch)
    {
        using var gpuContext = await _memoryPool.RentAsync();
        
        try
        {
            // Upload batch to GPU
            var gpuFrames = await UploadFramesToGpu(batch, gpuContext);
            
            // Process entire batch on GPU
            var results = await _recognitionEngine.ProcessBatchAsync(gpuFrames);
            
            // Process results and publish
            var publishTasks = results.Select(PublishResult);
            await Task.WhenAll(publishTasks);
        }
        catch (Exception ex)
        {
            // Handle batch processing failure
            await HandleBatchFailure(batch, ex);
        }
    }
}
```

## 10. Deployment & Infrastructure

### 10.1 Docker Configuration with GPU Support
```dockerfile
# Multi-stage build for Grabber Service
FROM nvidia/cuda:12.1-devel-ubuntu22.04 AS base

# Install system dependencies
RUN apt-get update && apt-get install -y \
    ffmpeg \
    libgstreamer1.0-0 \
    libgstreamer-plugins-base1.0-0 \
    libopencv-dev \
    python3-pip \
    && rm -rf /var/lib/apt/lists/*

# Install .NET runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
COPY --from=base / /

WORKDIR /app
EXPOSE 80 443 5672

# Set GPU environment
ENV NVIDIA_VISIBLE_DEVICES=all
ENV NVIDIA_DRIVER_CAPABILITIES=compute,utility

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GrabberService/GrabberService.csproj", "GrabberService/"]
RUN dotnet restore "GrabberService/GrabberService.csproj"

COPY . .
WORKDIR "/src/GrabberService"
RUN dotnet build "GrabberService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GrabberService.csproj" -c Release -o /app/publish

FROM runtime AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "GrabberService.dll"]
```

### 10.2 Kubernetes Deployment Configuration
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: grabber-service
  namespace: vms
spec:
  replicas: 3
  selector:
    matchLabels:
      app: grabber-service
  template:
    metadata:
      labels:
        app: grabber-service
    spec:
      nodeSelector:
        accelerator: nvidia-gpu
      containers:
      - name: grabber-service
        image: vms/grabber-service:latest
        ports:
        - containerPort: 80
        - containerPort: 443
        env:
        - name: INSTANCE_ID
          valueFrom:
            fieldRef:
              fieldPath: metadata.name
        - name: GPU_MEMORY_FRACTION
          value: "0.8"
        - name: RABBITMQ_CONNECTION
          valueFrom:
            secretKeyRef:
              name: rabbitmq-secret
              key: connection-string
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
        - name: camera-data
          mountPath: /app/data
        - name: model-cache
          mountPath: /app/models
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 10
      volumes:
      - name: camera-data
        persistentVolumeClaim:
          claimName: camera-data-pvc
      - name: model-cache
        emptyDir:
          sizeLimit: 10Gi
---
apiVersion: v1
kind: Service
metadata:
  name: grabber-service
  namespace: vms
spec:
  selector:
    app: grabber-service
  ports:
  - name: http
    port: 80
    targetPort: 80
  - name: https
    port: 443
    targetPort: 443
  type: ClusterIP
```

## 11. Testing Strategy

### 11.1 Integration Test Framework
```csharp
public class GrabberServiceIntegrationTests : IClassFixture<GrabberServiceFactory>
{
    private readonly GrabberServiceFactory _factory;
    private readonly HttpClient _client;
    
    public GrabberServiceIntegrationTests(GrabberServiceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task StartCamera_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var cameraConfig = new CameraStartRequest
        {
            CameraId = 1,
            CameraType = CameraType.RTSP,
            ConnectionString = "rtsp://test-camera:554/stream",
            EnableFacialRecognition = true,
            Configuration = new CameraConfiguration
            {
                ResolutionWidth = 1920,
                ResolutionHeight = 1080,
                FrameRate = 30
            }
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/cameras/start", cameraConfig);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CameraOperationResult>();
        
        Assert.True(result.Success);
        Assert.NotNull(result.SessionId);
        
        // Verify camera is actually processing
        await Task.Delay(2000); // Allow startup time
        
        var healthResponse = await _client.GetAsync($"/api/cameras/{cameraConfig.CameraId}/health");
        var healthResult = await healthResponse.Content.ReadFromJsonAsync<CameraHealthStatus>();
        
        Assert.Equal(CameraStatus.Active, healthResult.Status);
    }
    
    [Fact]
    public async Task ProcessFrameBatch_WithMultipleFrames_ProcessesAllFrames()
    {
        // Arrange
        var frames = GenerateTestFrames(8);
        var processingRequests = frames.Select(frame => new FrameProcessingRequest
        {
            CameraId = 1,
            FrameData = frame,
            EnableFacialRecognition = true,
            Timestamp = DateTime.UtcNow
        }).ToList();
        
        // Act
        var processingTasks = processingRequests.Select(async request =>
        {
            var response = await _client.PostAsJsonAsync("/api/frames/process", request);
            return await response.Content.ReadFromJsonAsync<FrameProcessingResult>();
        });
        
        var results = await Task.WhenAll(processingTasks);
        
        // Assert
        Assert.All(results, result => Assert.True(result.Success));
        Assert.True(results.All(r => r.ProcessingTimeMs < 200)); // Performance requirement
    }
}
```

### 11.2 Load Testing Configuration
```csharp
public class LoadTestScenarios
{
    [Fact]
    public async Task SimulateHighCameraLoad()
    {
        const int cameraCount = 50;
        const int durationMinutes = 10;
        
        var cameras = Enumerable.Range(1, cameraCount)
            .Select(id => new LoadTestCamera(id))
            .ToList();
        
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(durationMinutes));
        
        // Start all cameras simultaneously
        var startTasks = cameras.Select(camera => camera.StartAsync(cts.Token));
        await Task.WhenAll(startTasks);
        
        // Monitor performance during test
        var performanceMonitor = new PerformanceMonitor();
        var monitoringTask = performanceMonitor.MonitorAsync(cts.Token);
        
        // Simulate continuous frame processing
        var processingTasks = cameras.Select(camera => 
            camera.SimulateFrameProcessingAsync(cts.Token));
        
        await Task.WhenAll(processingTasks.Concat(new[] { monitoringTask }));
        
        // Verify performance requirements met
        var metrics = performanceMonitor.GetMetrics();
        
        Assert.True(metrics.AverageGpuUtilization > 0.8); // >80% GPU utilization
        Assert.True(metrics.AverageFrameProcessingTime < 100); // <100ms processing
        Assert.True(metrics.SuccessRate > 0.999); // 99.9% success rate
    }
}
```

---

## 12. Conclusion & Implementation Summary

### 12.1 Technical Architecture Benefits

The proposed Camera Frame Grabber Service architecture provides several key advantages over monolithic integration:

**Scalability & Performance**
- Horizontal scaling with GPU affinity for optimal resource utilization
- Dedicated processing power prevents camera operations from impacting main API
- Batch processing and memory pooling maximize GPU throughput
- Support for 50+ concurrent camera streams with sub-100ms latency

**Reliability & Resilience** 
- Isolated failure domains prevent camera issues from affecting core VMS
- Automatic recovery mechanisms with exponential backoff strategies
- Dead letter queue processing for failed message handling
- Health monitoring with real-time alerting

**Maintainability & Development**
- Clean separation of concerns with camera-specific functionality
- Consistent patterns matching existing VMS architecture (MediatR, SignalR)
- Comprehensive testing framework for validation and performance
- Production-ready monitoring and observability

### 12.2 Implementation Readiness

This PRD provides:
- **Complete technical specifications** for all service components
- **Production-quality code examples** demonstrating key patterns
- **Comprehensive error handling** with recovery strategies
- **Detailed deployment configurations** for containerized environments
- **Performance optimization techniques** for GPU resource management
- **Security implementation** with encryption and network hardening
- **Testing strategies** covering unit, integration, and load testing

### 12.3 Next Steps

The accompanying Planning Document (`planning.md`) provides:
- **Phase-by-phase implementation roadmap** with clear milestones
- **Task dependencies and sequencing** for efficient development
- **Resource allocation recommendations** for team planning
- **Risk mitigation strategies** for common implementation challenges
- **Delivery timeline estimates** based on team size and complexity

This architecture is designed to seamlessly integrate with your existing VMS infrastructure while providing the scalability and performance required for enterprise-grade camera management operations.

---

*Document Version: 1.0*  
*Last Updated: January 2025*  
*Author: Technical Architecture Team*
