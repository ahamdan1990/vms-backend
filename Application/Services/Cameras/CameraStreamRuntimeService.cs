using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Application.Services.VideoProcessing;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Process-local camera runtime. It owns FFmpeg workers and records live frame/inference evidence.
/// </summary>
public sealed class CameraStreamRuntimeService : ICameraStreamRuntimeService, IAsyncDisposable
{
    private readonly ConcurrentDictionary<int, CameraStreamWorkerState> _workers = new();
    private readonly ConcurrentDictionary<int, DateTimeOffset> _autoStartPausedCameraIds = new();
    private readonly ConcurrentDictionary<int, bool> _facialRecognitionOverrides = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<long, DateTime>> _trackerLastRecognition = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IFaceTrackerService _tracker;
    private readonly ILogger<CameraStreamRuntimeService> _logger;
    private bool _disposed;

    public CameraStreamRuntimeService(
        IServiceScopeFactory scopeFactory,
        IFaceTrackerService tracker,
        ILogger<CameraStreamRuntimeService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> StartStreamAsync(int cameraId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ResumeAutoStart(cameraId);

        if (_workers.TryGetValue(cameraId, out var existing) && existing.IsRunning)
        {
            _logger.LogInformation("Camera stream worker already running for camera {CameraId}", cameraId);
            return true;
        }

        if (existing?.WorkerTask?.IsCompleted == true)
        {
            _workers.TryRemove(cameraId, out _);
            existing.RequestStop(graceful: false);
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var camera = await unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);

        if (camera == null || camera.IsDeleted)
        {
            _logger.LogWarning("Cannot start camera stream worker. Camera {CameraId} was not found", cameraId);
            return false;
        }

        var config = camera.GetConfiguration();
        var state = _workers.GetOrAdd(cameraId, _ => new CameraStreamWorkerState(camera, config));
        state.UpdateCameraSnapshot(camera, config);

        if (camera.CameraType == CameraType.USB)
        {
            state.MarkClientSideOnly("USB cameras are captured by the browser client, not by backend FFmpeg.");
            _logger.LogInformation("Camera {CameraId} is a client-side USB source; backend FFmpeg worker was not started", cameraId);
            return false;
        }

        if (!camera.IsActive || !config.Enabled)
        {
            state.MarkFailed("Camera is inactive or its configuration is disabled");
            _logger.LogWarning("Cannot start camera stream worker for camera {CameraId}: inactive or disabled", cameraId);
            return false;
        }

        camera.UpdateStatus(CameraStatus.Connecting);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (state.TryStart())
        {
            state.WorkerTask = Task.Run(() => RunWorkerAsync(state), CancellationToken.None);
            _logger.LogInformation("Started camera stream worker for camera {CameraId}", cameraId);
        }

        return true;
    }

    public async Task<bool> StopStreamAsync(
        int cameraId,
        bool graceful = true,
        CancellationToken cancellationToken = default,
        bool pauseAutoStart = true)
    {
        ThrowIfDisposed();

        if (pauseAutoStart)
        {
            PauseAutoStart(cameraId);
        }

        if (!_workers.TryGetValue(cameraId, out var state))
        {
            return true;
        }

        state.RequestStop(graceful);

        if (state.WorkerTask != null)
        {
            try
            {
                await state.WorkerTask.WaitAsync(TimeSpan.FromSeconds(graceful ? 10 : 2), cancellationToken);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Timed out waiting for camera stream worker {CameraId} to stop", cameraId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Camera stream worker {CameraId} ended while stopping", cameraId);
            }
        }

        state.MarkStopped();
        _workers.TryRemove(cameraId, out var removed);
        removed?.Dispose();
        _logger.LogInformation("Stopped camera stream worker for camera {CameraId}", cameraId);
        return true;
    }

    public async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var cameraId in _workers.Keys.ToArray())
        {
            await StopStreamAsync(
                cameraId,
                graceful: true,
                cancellationToken: cancellationToken,
                pauseAutoStart: false);
        }
    }

    public bool IsStreaming(int cameraId)
    {
        return _workers.TryGetValue(cameraId, out var state) && state.GetInfo().IsStreaming;
    }

    public bool IsAutoStartPaused(int cameraId)
    {
        return _autoStartPausedCameraIds.ContainsKey(cameraId);
    }

    public void PauseAutoStart(int cameraId)
    {
        _autoStartPausedCameraIds[cameraId] = DateTimeOffset.UtcNow;
        _logger.LogInformation("Auto-start paused for camera {CameraId} until the stream is started manually or the application restarts", cameraId);
    }

    public void ResumeAutoStart(int cameraId)
    {
        if (_autoStartPausedCameraIds.TryRemove(cameraId, out _))
        {
            _logger.LogInformation("Auto-start pause cleared for camera {CameraId}", cameraId);
        }
    }

    public CameraStreamInfo? GetStreamInfo(int cameraId)
    {
        return _workers.TryGetValue(cameraId, out var state)
            ? state.GetInfo()
            : null;
    }

    public void RecordFrameCapture(CameraFrameCaptureResult result)
    {
        var state = _workers.GetOrAdd(
            result.CameraId,
            cameraId => CameraStreamWorkerState.CreateObservationOnly(cameraId));

        state.MarkSingleFrameCapture(result);
    }

    public void RecordInferenceResult(CameraFrameRecognitionResultDto result)
    {
        var state = _workers.GetOrAdd(
            result.CameraId,
            cameraId => CameraStreamWorkerState.CreateObservationOnly(cameraId));

        state.MarkInferenceCompleted(result);
    }

    public bool IsFacialRecognitionEnabled(int cameraId)
    {
        return !_facialRecognitionOverrides.TryGetValue(cameraId, out var disabled) || disabled;
    }

    public void SetFacialRecognitionEnabled(int cameraId, bool enabled)
    {
        if (enabled)
            _facialRecognitionOverrides.TryRemove(cameraId, out _);
        else
            _facialRecognitionOverrides[cameraId] = false;
    }

    public (byte[]? Bytes, DateTime? CapturedAt) GetLastFrameSnapshot(int cameraId)
    {
        return _workers.TryGetValue(cameraId, out var state)
            ? state.GetLastFrameSnapshot()
            : (null, null);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAllAsync(CancellationToken.None);
    }

    private async Task RunWorkerAsync(CameraStreamWorkerState state)
    {
        var cameraId = state.CameraId;
        var cancellationToken = state.CancellationToken;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                CameraConfiguration config;

                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var frameGrabber = scope.ServiceProvider.GetRequiredService<IFfmpegFrameGrabber>();
                    var recognitionService = scope.ServiceProvider.GetRequiredService<ICameraFrameRecognitionService>();
                    var eventService = scope.ServiceProvider.GetRequiredService<ICameraFaceEventService>();

                    var camera = await unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);
                    if (camera == null || camera.IsDeleted)
                    {
                        state.MarkFailed("Camera was deleted or no longer exists");
                        return;
                    }

                    config = camera.GetConfiguration();
                    state.UpdateCameraSnapshot(camera, config);

                    if (camera.CameraType == CameraType.USB)
                    {
                        state.MarkClientSideOnly("USB cameras are captured by the browser client, not by backend FFmpeg.");
                        return;
                    }

                    if (!camera.IsActive || !config.Enabled)
                    {
                        state.MarkFailed("Camera is inactive or configuration is disabled");
                        return;
                    }

                    state.MarkStarting();
                    var markedActive = false;

                    var useTracker = config.TrackingEnabled && _tracker.IsAvailable;
                    _logger.LogInformation(
                        "Camera {CameraId} stream loop: EnableFacialRecognition={FR}, DetectionEnabled={Det}, RecognitionEnabled={Rec}, TrackingEnabled={Track}, UseTracker={UseTracker}, TrackerAvailable={TrackerAvailable}",
                        cameraId,
                        camera.EnableFacialRecognition,
                        config.DetectionEnabled,
                        config.RecognitionEnabled,
                        config.TrackingEnabled,
                        useTracker,
                        _tracker.IsAvailable);
                    state.SetInferenceDiagnostics(useTracker, _tracker.IsAvailable);
                    if (useTracker && !_tracker.HasCameraTracker(camera.Id))
                    {
                        _tracker.CreateCameraTracker(
                            camera.Id,
                            maxFaces: Math.Max(1, config.MaxConcurrentTracks),
                            trackTimeoutMs: config.TrackTimeoutMs,
                            reIdTimeoutMs: config.ReIdentificationTimeoutMs);
                        _logger.LogDebug(
                            "Created Luxand tracker for camera {CameraId}. MaxFaces={MaxFaces}, TrackTimeout={TrackTimeout}ms, ReIdTimeout={ReIdTimeout}ms",
                            camera.Id,
                            config.MaxConcurrentTracks,
                            config.TrackTimeoutMs,
                            config.ReIdentificationTimeoutMs);
                    }

                    // Bounded channel with capacity 1 and DropOldest so the capture loop is never
                    // blocked by inference. When inference is slow the channel simply holds the
                    // latest un-processed frame; an arriving newer frame evicts the stale one.
                    var inferenceChannel = Channel.CreateBounded<(FfmpegFrame Frame, IReadOnlyList<TrackedFace> TrackedFaces)>(
                        new BoundedChannelOptions(1)
                        {
                            FullMode = BoundedChannelFullMode.DropOldest,
                            SingleReader = true,
                            SingleWriter = true
                        });

                    var inferenceTask = RunInferenceLoopAsync(
                        state, inferenceChannel.Reader, recognitionService, eventService, cancellationToken);

                    try
                    {
                        await foreach (var frame in frameGrabber.CaptureFramesAsync(camera, cancellationToken))
                        {
                            state.MarkFrameGrabbed(frame);
                            state.StoreLastFrame(frame.ImageBytes, frame.CapturedAt.UtcDateTime);

                            if (!markedActive)
                            {
                                camera.UpdateStatus(CameraStatus.Active);
                                await unitOfWork.SaveChangesAsync(cancellationToken);
                                markedActive = true;
                            }

                            IReadOnlyList<TrackedFace> trackedFaces = [];
                            bool shouldRunInference;

                            if (useTracker)
                            {
                                trackedFaces = await _tracker.FeedFrameAsync(camera.Id, frame.ImageBytes, cancellationToken);
                                // When tracker sees faces: use per-face recognition scheduling.
                                // When tracker sees no faces: fall back to the scheduled-interval path so
                                // direct detection still runs — the tracker may miss faces that the
                                // recognition engine's own detection step would catch.
                                shouldRunInference = trackedFaces.Count > 0
                                    ? ShouldRunInferenceForTracks(trackedFaces, camera.Id, config)
                                    : ShouldRunInference(camera, config, state, frame.CapturedAt);
                            }
                            else
                            {
                                shouldRunInference = ShouldRunInference(camera, config, state, frame.CapturedAt);
                            }

                            if (!shouldRunInference)
                            {
                                continue;
                            }

                            if (!inferenceChannel.Writer.TryWrite((frame, trackedFaces)))
                            {
                                _logger.LogDebug(
                                    "Camera {CameraId}: inference channel full — frame {Seq} dropped (inference is slower than capture rate)",
                                    cameraId, frame.SequenceNumber);
                            }
                        }
                    }
                    finally
                    {
                        inferenceChannel.Writer.TryComplete();
                        await inferenceTask;
                    }

                    if (useTracker)
                    {
                        _tracker.DeleteCameraTracker(camera.Id);
                        _trackerLastRecognition.TryRemove(camera.Id, out _);
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        state.MarkReconnect("FFmpeg stream ended without cancellation");
                        await DelayBeforeReconnectAsync(state, config, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    config = state.LastConfiguration ?? CameraConfiguration.Default;
                    state.MarkFailure(ex);

                    _logger.LogError(
                        ex,
                        "Camera stream worker failed for camera {CameraId}. FailureCount={FailureCount}",
                        cameraId,
                        state.GetInfo().FailureCount);

                    if (state.ConsecutiveFailures >= Math.Max(1, config.MaxRetryAttempts))
                    {
                        state.MarkFailed($"Maximum retry attempts reached: {ex.Message}");
                        await TryUpdateCameraStatusAsync(cameraId, CameraStatus.Error, ex.Message, CancellationToken.None);
                        return;
                    }

                    await DelayBeforeReconnectAsync(state, config, cancellationToken);
                }
            }
        }
        finally
        {
            if (cancellationToken.IsCancellationRequested)
            {
                state.MarkStopped();
            }
            else
            {
                state.MarkEnded();
            }

            // Always clean up the Luxand tracker for this camera on worker exit.
            if (_tracker.HasCameraTracker(cameraId))
            {
                _tracker.DeleteCameraTracker(cameraId);
            }

            _trackerLastRecognition.TryRemove(cameraId, out _);
        }
    }

    private bool ShouldRunInference(
        Camera camera,
        CameraConfiguration config,
        CameraStreamWorkerState state,
        DateTimeOffset capturedAt)
    {
        if (_facialRecognitionOverrides.TryGetValue(camera.Id, out var overrideEnabled) && !overrideEnabled)
        {
            _logger.LogDebug("CameraId={CameraId}: inference skipped — disabled by runtime override", camera.Id);
            return false;
        }

        if (!camera.EnableFacialRecognition)
        {
            _logger.LogDebug("CameraId={CameraId}: inference skipped — camera.EnableFacialRecognition=false", camera.Id);
            return false;
        }

        if (!config.DetectionEnabled && !config.RecognitionEnabled)
        {
            _logger.LogDebug("CameraId={CameraId}: inference skipped — both DetectionEnabled and RecognitionEnabled are false", camera.Id);
            return false;
        }

        var minimumIntervalMs = Math.Max(100, config.FrameSamplingIntervalMs);
        if (config.InferenceFps is > 0)
        {
            minimumIntervalMs = Math.Max(
                minimumIntervalMs,
                (int)Math.Ceiling(1000d / config.InferenceFps.Value));
        }

        var lastInferenceAt = state.LastInferenceCompletedAt;
        if (lastInferenceAt != null &&
            capturedAt.UtcDateTime < lastInferenceAt.Value.AddMilliseconds(minimumIntervalMs))
        {
            _logger.LogDebug(
                "CameraId={CameraId}: inference skipped — too soon. MinIntervalMs={MinInterval}, MsSinceLast={MsSinceLast}",
                camera.Id,
                minimumIntervalMs,
                (capturedAt.UtcDateTime - lastInferenceAt.Value).TotalMilliseconds);
            return false;
        }

        return true;
    }

    private bool ShouldRunInferenceForTracks(
        IReadOnlyList<TrackedFace> trackedFaces,
        int cameraId,
        CameraConfiguration config)
    {
        if (trackedFaces.Count == 0)
        {
            return false;
        }

        var perCamera = _trackerLastRecognition.GetOrAdd(cameraId, _ => new ConcurrentDictionary<long, DateTime>());
        var intervalMs = Math.Max(0, config.RecognitionIntervalPerTrackMs);
        var now = DateTime.UtcNow;

        // Evict face IDs that the tracker has retired (not seen for longer than TrackTimeoutMs).
        // Without this the dictionary grows indefinitely over long sessions.
        var staleThreshold = now.AddMilliseconds(-(Math.Max(config.TrackTimeoutMs, 10_000) * 2));
        foreach (var staleId in perCamera
                     .Where(kvp => kvp.Value < staleThreshold)
                     .Select(kvp => kvp.Key)
                     .ToList())
        {
            perCamera.TryRemove(staleId, out _);
        }

        foreach (var face in trackedFaces)
        {
            if (!perCamera.TryGetValue(face.FaceId, out var lastAt))
            {
                return true; // New face ID seen for the first time
            }

            if (intervalMs <= 0 || (now - lastAt).TotalMilliseconds >= intervalMs)
            {
                return true; // Face is due for re-recognition
            }
        }

        return false;
    }

    private void MarkTrackerRecognitionDone(int cameraId, IReadOnlyList<TrackedFace> trackedFaces)
    {
        var perCamera = _trackerLastRecognition.GetOrAdd(cameraId, _ => new ConcurrentDictionary<long, DateTime>());
        var now = DateTime.UtcNow;
        foreach (var face in trackedFaces)
        {
            perCamera[face.FaceId] = now;
        }
    }

    private async Task RunInferenceLoopAsync(
        CameraStreamWorkerState state,
        ChannelReader<(FfmpegFrame Frame, IReadOnlyList<TrackedFace> TrackedFaces)> reader,
        ICameraFrameRecognitionService recognitionService,
        ICameraFaceEventService eventService,
        CancellationToken cancellationToken)
    {
        await foreach (var (frame, trackedFaces) in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await RunInferenceAsync(state, frame, recognitionService, eventService, cancellationToken);

                if (trackedFaces.Count > 0)
                {
                    MarkTrackerRecognitionDone(frame.CameraId, trackedFaces);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Camera {CameraId}: inference failed for frame {Seq}. Capture loop continues.",
                    frame.CameraId, frame.SequenceNumber);
            }
        }
    }

    private async Task RunInferenceAsync(
        CameraStreamWorkerState state,
        FfmpegFrame frame,
        ICameraFrameRecognitionService recognitionService,
        ICameraFaceEventService eventService,
        CancellationToken cancellationToken)
    {
        state.MarkInferenceStarted();

        var hwAccel = GetMetadataValue(frame, "hardwareAcceleration", "unknown");
        var inputKind = GetMetadataValue(frame, "inputKind", "stream");

        _logger.LogDebug(
            "Camera inference starting. CameraId={CameraId}, Sequence={Sequence}, FrameSize={FrameSize} bytes, HardwareAcceleration={HwAccel}, InputKind={InputKind}",
            frame.CameraId,
            frame.SequenceNumber,
            frame.ImageBytes.Length,
            hwAccel,
            inputKind);

        await using var frameStream = new MemoryStream(frame.ImageBytes, writable: false);
        var formFile = new FormFile(frameStream, 0, frame.ImageBytes.Length, "Frame", $"camera-{frame.CameraId}-{frame.SequenceNumber}.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = frame.ContentType
        };

        var request = new CameraFrameProcessingRequestDto
        {
            Frame = formFile,
            CapturedAt = frame.CapturedAt.UtcDateTime,
            SourceDeviceId = $"ffmpeg:{inputKind}"
        };

        var result = await recognitionService.ProcessFrameAsync(
            frame.CameraId,
            request,
            processedByUserId: null,
            clientIpAddress: "ffmpeg-worker",
            cancellationToken);

        if (result.Success)
        {
            var events = await eventService.PublishFrameEventsAsync(result, processedByUserId: null, cancellationToken);
            result.Events = events.ToList();
        }

        state.MarkInferenceCompleted(result);

        if (result.RecognitionSkipped)
        {
            _logger.LogDebug(
                "Camera inference skipped. CameraId={CameraId}, Sequence={Sequence}, Reason={Reason}",
                frame.CameraId, frame.SequenceNumber, result.SkipReason);
        }
        else
        {
            _logger.LogInformation(
                "Camera inference completed. CameraId={CameraId}, Sequence={Sequence}, Engine={Engine}, Detected={Detected}, Known={Known}, Unknown={Unknown}, Events={Events}, Success={Success}, Error={Error}",
                frame.CameraId,
                frame.SequenceNumber,
                result.EngineUsed,
                result.DetectedFaceCount,
                result.KnownFaceCount,
                result.UnknownFaceCount,
                result.Events.Count,
                result.Success,
                result.ErrorMessage);
        }
    }

    private static async Task DelayBeforeReconnectAsync(
        CameraStreamWorkerState state,
        CameraConfiguration config,
        CancellationToken cancellationToken)
    {
        var delaySeconds = Math.Clamp(config.RetryIntervalSeconds, 5, 300);
        state.MarkReconnectDelay(TimeSpan.FromSeconds(delaySeconds));
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
    }

    private async Task TryUpdateCameraStatusAsync(
        int cameraId,
        CameraStatus status,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var camera = await unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);

            if (camera == null || camera.IsDeleted)
            {
                return;
            }

            camera.UpdateStatus(status, errorMessage);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update camera {CameraId} status to {Status}", cameraId, status);
        }
    }

    private static string GetMetadataValue(FfmpegFrame frame, string key, string fallback)
    {
        return frame.Metadata.TryGetValue(key, out var value)
            ? value?.ToString() ?? fallback
            : fallback;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CameraStreamRuntimeService));
        }
    }

    private sealed class CameraStreamWorkerState : IDisposable
    {
        private readonly object _sync = new();
        private readonly CancellationTokenSource _cts = new();
        private CameraStreamInfo _info;
        private bool _started;

        // Last grabbed frame — stored for debug inspection only; eventual consistency is fine.
        private byte[]? _lastFrameBytes;
        private DateTime _lastFrameCapturedAt;

        private CameraStreamWorkerState(CameraStreamInfo info)
        {
            _info = info;
        }

        public CameraStreamWorkerState(Camera camera, CameraConfiguration config)
            : this(CreateInfo(camera, config, isStreaming: false, runtimeState: "stopped"))
        {
            LastConfiguration = config;
        }

        public int CameraId => _info.CameraId;

        public Task? WorkerTask { get; set; }

        public CancellationToken CancellationToken => _cts.Token;

        public CameraConfiguration? LastConfiguration { get; private set; }

        public int ConsecutiveFailures { get; private set; }

        public DateTime? LastInferenceCompletedAt
        {
            get
            {
                lock (_sync)
                {
                    return _info.LastInferenceCompletedAt;
                }
            }
        }

        public bool IsRunning
        {
            get
            {
                lock (_sync)
                {
                    return _info.IsStreaming && !_cts.IsCancellationRequested;
                }
            }
        }

        public static CameraStreamWorkerState CreateObservationOnly(int cameraId)
        {
            return new CameraStreamWorkerState(new CameraStreamInfo
            {
                CameraId = cameraId,
                IsStreaming = false,
                RuntimeState = "observed",
                StartedAt = null
            });
        }

        public bool TryStart()
        {
            lock (_sync)
            {
                if (_started && !_cts.IsCancellationRequested)
                {
                    return false;
                }

                _started = true;
                _info.IsStreaming = true;
                _info.IsFrameGrabberActive = false;
                _info.RuntimeState = "starting";
                _info.StartedAt = DateTime.UtcNow;
                _info.LastErrorMessage = null;
                _info.LastErrorAt = null;
                _info.NextReconnectAt = null;
                return true;
            }
        }

        public void UpdateCameraSnapshot(Camera camera, CameraConfiguration config)
        {
            lock (_sync)
            {
                LastConfiguration = config;
                var existing = _info;
                _info = CreateInfo(camera, config, existing.IsStreaming, existing.RuntimeState);
                CopyRuntimeFields(existing, _info);
            }
        }

        public void RequestStop(bool graceful)
        {
            lock (_sync)
            {
                _info.RuntimeState = graceful ? "stopping" : "terminating";
            }

            _cts.Cancel();
        }

        public void MarkClientSideOnly(string message)
        {
            lock (_sync)
            {
                _info.IsStreaming = false;
                _info.IsFrameGrabberActive = false;
                _info.IsInferenceRunning = false;
                _info.RuntimeState = "clientSide";
                _info.LastErrorMessage = message;
                _info.Metadata["clientSide"] = true;
            }
        }

        public void MarkStarting()
        {
            lock (_sync)
            {
                _info.IsStreaming = true;
                _info.IsFrameGrabberActive = true;
                _info.RuntimeState = "starting";
                _info.LastErrorMessage = null;
                _info.LastErrorAt = null;
                _info.NextReconnectAt = null;
            }
        }

        public void SetInferenceDiagnostics(bool useTracker, bool trackerAvailable)
        {
            lock (_sync)
            {
                _info.Metadata["useTracker"] = useTracker;
                _info.Metadata["trackerAvailable"] = trackerAvailable;
            }
        }

        public void MarkFrameGrabbed(FfmpegFrame frame)
        {
            lock (_sync)
            {
                ConsecutiveFailures = 0;
                _info.IsStreaming = true;
                _info.IsFrameGrabberActive = true;
                _info.RuntimeState = "running";
                _info.LastFrameGrabbedAt = frame.CapturedAt.UtcDateTime;
                _info.LastFrameSequenceNumber = frame.SequenceNumber;
                _info.GrabbedFrameCount++;
                _info.LastFrameSizeBytes = frame.ImageBytes.Length;
                _info.LastFrameHardwareAcceleration = GetMetadata(frame, "hardwareAcceleration");
                _info.LastFrameInputKind = GetMetadata(frame, "inputKind");
                _info.LastFfmpegArguments = GetMetadata(frame, "arguments");
                _info.LastErrorMessage = null;
                _info.LastErrorAt = null;
                _info.NextReconnectAt = null;
            }
        }

        public void MarkSingleFrameCapture(CameraFrameCaptureResult result)
        {
            lock (_sync)
            {
                _info.LastFrameGrabbedAt = result.CapturedAt.UtcDateTime;
                _info.GrabbedFrameCount++;
                _info.LastFrameSizeBytes = result.FrameSizeBytes;
                _info.LastFrameHardwareAcceleration = result.HardwareAcceleration;
                _info.LastFfmpegArguments = result.Details.TryGetValue("arguments", out var arguments)
                    ? arguments?.ToString()
                    : _info.LastFfmpegArguments;

                if (result.Details.TryGetValue("inputKind", out var inputKind))
                {
                    _info.LastFrameInputKind = inputKind?.ToString();
                }

                if (result.IsSuccess)
                {
                    _info.RuntimeState = _info.IsStreaming ? "running" : "frameProbeSucceeded";
                    _info.LastErrorMessage = null;
                    _info.LastErrorAt = null;
                    return;
                }

                _info.RuntimeState = _info.IsStreaming ? "runningWithFrameProbeError" : "frameProbeFailed";
                _info.LastErrorMessage = result.ErrorMessage;
                _info.LastErrorAt = DateTime.UtcNow;
                _info.FailureCount++;
            }
        }

        public void MarkInferenceStarted()
        {
            lock (_sync)
            {
                _info.IsInferenceRunning = true;
                _info.LastInferenceStartedAt = DateTime.UtcNow;
            }
        }

        public void MarkInferenceCompleted(CameraFrameRecognitionResultDto result)
        {
            lock (_sync)
            {
                _info.IsInferenceRunning = false;
                _info.LastInferenceCompletedAt = result.ProcessedAt;
                _info.InferenceFrameCount++;
                _info.LastInferenceEngine = result.EngineUsed;
                _info.LastInferenceFallbackUsed = result.FallbackUsed;
                _info.LastInferenceSkipped = result.RecognitionSkipped;
                _info.LastInferenceSkipReason = result.SkipReason;
                _info.LastDetectedFaceCount = result.DetectedFaceCount;
                _info.LastRecognizedFaceCount = result.RecognizedFaceCount;
                _info.LastKnownFaceCount = result.KnownFaceCount;
                _info.LastUnknownFaceCount = result.UnknownFaceCount;
                _info.LastPublishedEventCount = result.Events.Count(e => e.Emitted);
                _info.Metadata["lastEngineStatus"] = new Dictionary<string, object>(result.EngineStatus);

                if (result.Success)
                {
                    _info.LastErrorMessage = null;
                    _info.LastErrorAt = null;
                }
                else
                {
                    _info.LastErrorMessage = result.ErrorMessage;
                    _info.LastErrorAt = DateTime.UtcNow;
                }
            }
        }

        public void MarkReconnect(string reason)
        {
            lock (_sync)
            {
                _info.IsFrameGrabberActive = false;
                _info.IsInferenceRunning = false;
                _info.RuntimeState = "reconnecting";
                _info.LastErrorMessage = reason;
                _info.LastErrorAt = DateTime.UtcNow;
                _info.FailureCount++;
                ConsecutiveFailures++;
            }
        }

        public void MarkReconnectDelay(TimeSpan delay)
        {
            lock (_sync)
            {
                _info.NextReconnectAt = DateTime.UtcNow.Add(delay);
            }
        }

        public void MarkFailure(Exception ex)
        {
            lock (_sync)
            {
                _info.IsFrameGrabberActive = false;
                _info.IsInferenceRunning = false;
                _info.RuntimeState = "reconnecting";
                _info.LastErrorMessage = ex.Message;
                _info.LastErrorAt = DateTime.UtcNow;
                _info.FailureCount++;
                ConsecutiveFailures++;
            }
        }

        public void MarkFailed(string message)
        {
            lock (_sync)
            {
                _info.IsStreaming = false;
                _info.IsFrameGrabberActive = false;
                _info.IsInferenceRunning = false;
                _info.RuntimeState = "failed";
                _info.LastErrorMessage = message;
                _info.LastErrorAt = DateTime.UtcNow;
                _info.NextReconnectAt = null;
                _info.FailureCount++;
            }
        }

        public void MarkStopped()
        {
            lock (_sync)
            {
                _info.IsStreaming = false;
                _info.IsFrameGrabberActive = false;
                _info.IsInferenceRunning = false;
                _info.RuntimeState = "stopped";
                _info.NextReconnectAt = null;
            }
        }

        public void MarkEnded()
        {
            lock (_sync)
            {
                _info.IsStreaming = false;
                _info.IsFrameGrabberActive = false;
                _info.IsInferenceRunning = false;
                _info.NextReconnectAt = null;
                if (!string.Equals(_info.RuntimeState, "failed", StringComparison.OrdinalIgnoreCase))
                {
                    _info.RuntimeState = "stopped";
                }
            }
        }

        public CameraStreamInfo GetInfo()
        {
            lock (_sync)
            {
                return CloneInfo(_info);
            }
        }

        private static CameraStreamInfo CreateInfo(
            Camera camera,
            CameraConfiguration config,
            bool isStreaming,
            string runtimeState)
        {
            return new CameraStreamInfo
            {
                CameraId = camera.Id,
                IsStreaming = isStreaming,
                RuntimeState = runtimeState,
                StreamUrl = camera.GetSafeConnectionString(),
                CurrentFrameRate = config.CaptureFpsLimit ?? config.FrameRate,
                CurrentWidth = config.ResolutionWidth,
                CurrentHeight = config.ResolutionHeight,
                ActiveConnections = 0,
                StartedAt = isStreaming ? DateTime.UtcNow : null,
                Metadata =
                {
                    ["cameraType"] = camera.CameraType.ToString(),
                    ["cameraRole"] = config.CameraRole.ToString(),
                    ["workflowMode"] = config.WorkflowMode.ToString(),
                    ["preferredEngine"] = config.PreferredEngine.ToString(),
                    ["inferenceFps"] = config.InferenceFps ?? 0,
                    ["frameSamplingIntervalMs"] = config.FrameSamplingIntervalMs,
                    ["gpuDecodingEnabled"] = config.GpuDecodingEnabled,
                    ["cpuFallbackEnabled"] = config.CpuFallbackEnabled,
                    ["hardwareAcceleration"] = config.HardwareAcceleration.ToString(),
                    // inference eligibility flags — visible in stream-info for diagnostics
                    ["cameraEnableFacialRecognition"] = camera.EnableFacialRecognition,
                    ["configEnabled"] = config.Enabled,
                    ["detectionEnabled"] = config.DetectionEnabled,
                    ["recognitionEnabled"] = config.RecognitionEnabled,
                    ["trackingEnabled"] = config.TrackingEnabled
                }
            };
        }

        private static void CopyRuntimeFields(CameraStreamInfo source, CameraStreamInfo target)
        {
            target.StartedAt = source.StartedAt;
            target.LastFrameGrabbedAt = source.LastFrameGrabbedAt;
            target.LastFrameSequenceNumber = source.LastFrameSequenceNumber;
            target.GrabbedFrameCount = source.GrabbedFrameCount;
            target.LastFrameSizeBytes = source.LastFrameSizeBytes;
            target.LastFrameHardwareAcceleration = source.LastFrameHardwareAcceleration;
            target.LastFrameInputKind = source.LastFrameInputKind;
            target.LastFfmpegArguments = source.LastFfmpegArguments;
            target.LastInferenceStartedAt = source.LastInferenceStartedAt;
            target.LastInferenceCompletedAt = source.LastInferenceCompletedAt;
            target.InferenceFrameCount = source.InferenceFrameCount;
            target.LastInferenceEngine = source.LastInferenceEngine;
            target.LastInferenceFallbackUsed = source.LastInferenceFallbackUsed;
            target.LastInferenceSkipped = source.LastInferenceSkipped;
            target.LastInferenceSkipReason = source.LastInferenceSkipReason;
            target.LastDetectedFaceCount = source.LastDetectedFaceCount;
            target.LastRecognizedFaceCount = source.LastRecognizedFaceCount;
            target.LastKnownFaceCount = source.LastKnownFaceCount;
            target.LastUnknownFaceCount = source.LastUnknownFaceCount;
            target.LastPublishedEventCount = source.LastPublishedEventCount;
            target.LastErrorAt = source.LastErrorAt;
            target.LastErrorMessage = source.LastErrorMessage;
            target.FailureCount = source.FailureCount;
            target.NextReconnectAt = source.NextReconnectAt;
            target.IsFrameGrabberActive = source.IsFrameGrabberActive;
            target.IsInferenceRunning = source.IsInferenceRunning;
        }

        private static CameraStreamInfo CloneInfo(CameraStreamInfo source)
        {
            return new CameraStreamInfo
            {
                CameraId = source.CameraId,
                IsStreaming = source.IsStreaming,
                StreamUrl = source.StreamUrl,
                CurrentFrameRate = source.CurrentFrameRate,
                CurrentWidth = source.CurrentWidth,
                CurrentHeight = source.CurrentHeight,
                ActiveConnections = source.ActiveConnections,
                StartedAt = source.StartedAt,
                QualityScore = source.QualityScore,
                Metadata = new Dictionary<string, object>(source.Metadata),
                RuntimeState = source.RuntimeState,
                IsFrameGrabberActive = source.IsFrameGrabberActive,
                IsInferenceRunning = source.IsInferenceRunning,
                LastFrameGrabbedAt = source.LastFrameGrabbedAt,
                LastFrameSequenceNumber = source.LastFrameSequenceNumber,
                GrabbedFrameCount = source.GrabbedFrameCount,
                LastFrameSizeBytes = source.LastFrameSizeBytes,
                LastFrameHardwareAcceleration = source.LastFrameHardwareAcceleration,
                LastFrameInputKind = source.LastFrameInputKind,
                LastFfmpegArguments = source.LastFfmpegArguments,
                LastInferenceStartedAt = source.LastInferenceStartedAt,
                LastInferenceCompletedAt = source.LastInferenceCompletedAt,
                InferenceFrameCount = source.InferenceFrameCount,
                LastInferenceEngine = source.LastInferenceEngine,
                LastInferenceFallbackUsed = source.LastInferenceFallbackUsed,
                LastInferenceSkipped = source.LastInferenceSkipped,
                LastInferenceSkipReason = source.LastInferenceSkipReason,
                LastDetectedFaceCount = source.LastDetectedFaceCount,
                LastRecognizedFaceCount = source.LastRecognizedFaceCount,
                LastKnownFaceCount = source.LastKnownFaceCount,
                LastUnknownFaceCount = source.LastUnknownFaceCount,
                LastPublishedEventCount = source.LastPublishedEventCount,
                LastErrorAt = source.LastErrorAt,
                LastErrorMessage = source.LastErrorMessage,
                FailureCount = source.FailureCount,
                NextReconnectAt = source.NextReconnectAt
            };
        }

        public void StoreLastFrame(byte[] bytes, DateTime capturedAt)
        {
            var copy = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);
            _lastFrameCapturedAt = capturedAt;
            Volatile.Write(ref _lastFrameBytes, copy);
        }

        public (byte[]? Bytes, DateTime? CapturedAt) GetLastFrameSnapshot()
        {
            var bytes = Volatile.Read(ref _lastFrameBytes);
            if (bytes == null)
            {
                return (null, null);
            }

            var copy = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);
            return (copy, _lastFrameCapturedAt);
        }

        public void Dispose()
        {
            _cts.Dispose();
        }

        private static string? GetMetadata(FfmpegFrame frame, string key)
        {
            return frame.Metadata.TryGetValue(key, out var value)
                ? value?.ToString()
                : null;
        }
    }
}
