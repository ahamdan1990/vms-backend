using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;
using VisitorManagementSystem.Api.Hubs;

// IFaceSnapshotService lives in the same Application.Services.Cameras namespace.

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Applies short-term duplicate suppression and publishes receptionist/security face events.
/// This is intentionally stateless beyond memory-cache cooldowns; durable movement/workflow state
/// belongs in the later check-in/check-out pipeline phase.
/// </summary>
public class CameraFaceEventService : ICameraFaceEventService
{
    private const string SignalREventName = "CameraFaceEvent";
    private const double UnknownTrackIouThreshold = 0.25;

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private const int KnownCandidateMaxSnapshots = 5;
    private const int UnknownRetentionDays = 7;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _memoryCache;
    private readonly IFaceSnapshotService _snapshotService;
    private readonly IHubContext<OperatorHub> _operatorHubContext;
    private readonly IHubContext<SecurityHub> _securityHubContext;
    private readonly IHubContext<AdminHub> _adminHubContext;
    private readonly ILogger<CameraFaceEventService> _logger;

    public CameraFaceEventService(
        IUnitOfWork unitOfWork,
        IMemoryCache memoryCache,
        IFaceSnapshotService snapshotService,
        IHubContext<OperatorHub> operatorHubContext,
        IHubContext<SecurityHub> securityHubContext,
        IHubContext<AdminHub> adminHubContext,
        ILogger<CameraFaceEventService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _operatorHubContext = operatorHubContext ?? throw new ArgumentNullException(nameof(operatorHubContext));
        _securityHubContext = securityHubContext ?? throw new ArgumentNullException(nameof(securityHubContext));
        _adminHubContext = adminHubContext ?? throw new ArgumentNullException(nameof(adminHubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<CameraFrameFaceEventDto>> PublishFrameEventsAsync(
        CameraFrameRecognitionResultDto recognitionResult,
        int? processedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (!ShouldPublish(recognitionResult))
        {
            return Array.Empty<CameraFrameFaceEventDto>();
        }

        var camera = await _unitOfWork.Cameras.GetByIdAsync(recognitionResult.CameraId, cancellationToken);
        if (camera == null)
        {
            return Array.Empty<CameraFrameFaceEventDto>();
        }

        var config = camera.GetConfiguration();
        var events = new List<CameraFrameFaceEventDto>();

        foreach (var face in recognitionResult.Faces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var eventDto = CreateEventDto(recognitionResult, face);
            var cooldownDecision = ApplyCooldown(recognitionResult, face, config);

            if (!cooldownDecision.ShouldEmit)
            {
                eventDto.Emitted = false;
                eventDto.SuppressedReason = cooldownDecision.Reason;
                eventDto.NextAllowedAt = cooldownDecision.NextAllowedAt;
                events.Add(eventDto);
                continue;
            }

            var payload = CreatePayload(recognitionResult, face, eventDto);
            var alert = CreateAlert(payload, recognitionResult, face, processedByUserId);

            // Persist durable face event with snapshot before saving alert (gets Id after save).
            var faceEvent = await CreateFaceEventRecordAsync(
                recognitionResult, face, eventDto.EventId, recognitionResult.FrameBytes, cancellationToken);

            await _unitOfWork.Repository<NotificationAlert>().AddAsync(alert, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Back-link the alert to the face event so review panel can open the alert.
            if (faceEvent != null)
            {
                faceEvent.AlertId = alert.Id;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                eventDto.FaceEventDbId = faceEvent.Id;
                payload.FaceEventDbId = faceEvent.Id;
                payload.Actions = BuildActions(face.SuggestedAction, eventDto.EventId, recognitionResult.CameraId, faceEvent.Id);
            }

            payload.AlertId = alert.Id;
            eventDto.AlertId = alert.Id;
            eventDto.Emitted = true;

            await PublishSignalREventAsync(payload, face, cancellationToken);
            MarkCooldownEmitted(recognitionResult, face, config, cooldownDecision.Cooldown);

            events.Add(eventDto);

            _logger.LogInformation(
                "Published camera face event. EventId={EventId}, AlertId={AlertId}, CameraId={CameraId}, EventType={EventType}, PersonType={PersonType}, PersonId={PersonId}",
                payload.EventId,
                alert.Id,
                recognitionResult.CameraId,
                payload.EventType,
                face.PersonType,
                face.PersonId);
        }

        return events;
    }

    private static bool ShouldPublish(CameraFrameRecognitionResultDto result)
    {
        return result.Success &&
               !result.RecognitionSkipped &&
               result.Faces.Count > 0 &&
               result.Faces.Any(face => !string.Equals(face.SuggestedAction, "none", StringComparison.OrdinalIgnoreCase));
    }

    private CooldownDecision ApplyCooldown(
        CameraFrameRecognitionResultDto result,
        CameraFrameFaceResultDto face,
        CameraConfiguration config)
    {
        var now = DateTime.UtcNow;
        var identityKey = BuildIdentityKey(result, face, config, now);
        var activeKey = $"camera-face:active:{identityKey}";
        var cooldownKey = $"camera-face:cooldown:{identityKey}";
        var trackWindow = GetTrackWindow(config);
        var cooldown = GetEventCooldown(config, face);

        if (_memoryCache.TryGetValue<FaceActiveState>(activeKey, out var activeState) &&
            activeState != null &&
            activeState.LastSeenAt.Add(trackWindow) > now)
        {
            activeState.LastSeenAt = now;
            _memoryCache.Set(activeKey, activeState, trackWindow);

            return new CooldownDecision(
                ShouldEmit: false,
                Reason: "activeTrackCooldown",
                NextAllowedAt: now.Add(trackWindow),
                Cooldown: cooldown);
        }

        if (_memoryCache.TryGetValue<DateTime>(cooldownKey, out var nextAllowedAt) &&
            nextAllowedAt > now)
        {
            _memoryCache.Set(activeKey, new FaceActiveState(now), trackWindow);

            return new CooldownDecision(
                ShouldEmit: false,
                Reason: "alertCooldown",
                NextAllowedAt: nextAllowedAt,
                Cooldown: cooldown);
        }

        _memoryCache.Set(activeKey, new FaceActiveState(now), trackWindow);

        return new CooldownDecision(
            ShouldEmit: true,
            Reason: null,
            NextAllowedAt: null,
            Cooldown: cooldown);
    }

    private void MarkCooldownEmitted(
        CameraFrameRecognitionResultDto result,
        CameraFrameFaceResultDto face,
        CameraConfiguration config,
        TimeSpan cooldown)
    {
        var identityKey = BuildIdentityKey(result, face, config, DateTime.UtcNow);
        var cooldownKey = $"camera-face:cooldown:{identityKey}";
        var nextAllowedAt = DateTime.UtcNow.Add(cooldown);

        _memoryCache.Set(cooldownKey, nextAllowedAt, cooldown);
        _memoryCache.Set(
            $"camera-face:active:{identityKey}",
            new FaceActiveState(DateTime.UtcNow),
            GetTrackWindow(config));
    }

    private async Task PublishSignalREventAsync(
        CameraFaceEventPayload payload,
        CameraFrameFaceResultDto face,
        CancellationToken cancellationToken)
    {
        var publishTasks = new List<Task>
        {
            _operatorHubContext.Clients.Group("Operators")
                .SendAsync(SignalREventName, payload, cancellationToken),
            _adminHubContext.Clients.Group("Administrators")
                .SendAsync(SignalREventName, payload, cancellationToken)
        };

        if (face.IsBlacklisted)
        {
            publishTasks.Add(
                _securityHubContext.Clients.Group("Security")
                    .SendAsync(SignalREventName, payload, cancellationToken));
        }

        await Task.WhenAll(publishTasks);
    }

    private static NotificationAlert CreateAlert(
        CameraFaceEventPayload payload,
        CameraFrameRecognitionResultDto result,
        CameraFrameFaceResultDto face,
        int? processedByUserId)
    {
        var payloadJson = JsonSerializer.Serialize(payload, PayloadJsonOptions);

        return NotificationAlert.CreateFRAlert(
            payload.Title,
            payload.Message,
            GetNotificationAlertType(face),
            GetPriority(face),
            targetRole: UserRoles.Receptionist,
            targetLocationId: result.CameraLocationId,
            relatedEntityType: face.IsKnown ? face.PersonType : "Camera",
            relatedEntityId: face.IsKnown ? face.PersonId : result.CameraId,
            payloadData: payloadJson,
            createdBy: processedByUserId);
    }

    private static CameraFrameFaceEventDto CreateEventDto(
        CameraFrameRecognitionResultDto result,
        CameraFrameFaceResultDto face)
    {
        var priority = GetPriority(face).ToString();
        var eventType = GetEventType(face);

        return new CameraFrameFaceEventDto
        {
            FaceIndex = face.Index,
            EventId = Guid.NewGuid().ToString("N"),
            EventType = eventType,
            Emitted = false,
            Title = GetTitle(face),
            Message = GetMessage(result, face),
            Priority = priority,
            SuggestedAction = face.SuggestedAction
        };
    }

    private static CameraFaceEventPayload CreatePayload(
        CameraFrameRecognitionResultDto result,
        CameraFrameFaceResultDto face,
        CameraFrameFaceEventDto eventDto)
    {
        return new CameraFaceEventPayload
        {
            EventId = eventDto.EventId,
            EventType = eventDto.EventType,
            AlertType = GetModalAlertType(face),
            Title = eventDto.Title,
            Message = eventDto.Message,
            Priority = eventDto.Priority,
            RequiresAcknowledgement = RequiresAcknowledgement(result, face),
            SuggestedAction = face.SuggestedAction,
            SuggestedActionLabel = GetSuggestedActionLabel(face.SuggestedAction),
            Actions = BuildActions(face.SuggestedAction, eventDto.EventId, result.CameraId, null),
            CameraId = result.CameraId,
            CameraName = result.CameraName,
            CameraLocationId = result.CameraLocationId,
            CameraLocationName = result.CameraLocationName,
            CameraType = result.CameraType.ToString(),
            CameraRole = result.CameraRole.ToString(),
            WorkflowMode = result.WorkflowMode.ToString(),
            CapturedAt = result.CapturedAt,
            ProcessedAt = result.ProcessedAt,
            EngineUsed = result.EngineUsed,
            FallbackUsed = result.FallbackUsed,
            SourceDeviceId = result.SourceDeviceId,
            ClientTrackId = result.ClientTrackId,
            Face = face,
            Timestamp = DateTime.UtcNow
        };
    }

    private static bool RequiresAcknowledgement(
        CameraFrameRecognitionResultDto result,
        CameraFrameFaceResultDto face)
    {
        if (face.IsBlacklisted || !face.IsKnown)
        {
            return true;
        }

        return result.WorkflowMode == CameraWorkflowMode.Manual &&
               result.CameraRole is CameraRole.Entry or CameraRole.Exit;
    }

    private static List<CameraFaceEventAction> BuildActions(string? suggestedAction, string eventId, int cameraId, int? faceEventDbId = null)
    {
        var faceParam = faceEventDbId.HasValue ? $"&faceEventId={faceEventDbId.Value}" : string.Empty;
        var receptionHref = $"/receptionist?cameraEventId={Uri.EscapeDataString(eventId)}&cameraId={cameraId}{faceParam}";

        return suggestedAction switch
        {
            "confirmCheckIn" => new List<CameraFaceEventAction>
            {
                new("confirmCheckIn", "Open Check-In", receptionHref)
            },
            "confirmCheckOut" => new List<CameraFaceEventAction>
            {
                new("confirmCheckOut", "Open Check-Out", receptionHref)
            },
            "autoCheckIn" => new List<CameraFaceEventAction>
            {
                new("viewReception", "View Reception", receptionHref)
            },
            "autoCheckOut" => new List<CameraFaceEventAction>
            {
                new("viewReception", "View Reception", receptionHref)
            },
            "reviewUnknown" or "reviewUnknownFaces" => new List<CameraFaceEventAction>
            {
                new("reviewUnknown", "Review Person", receptionHref)
            },
            "securityAlert" => new List<CameraFaceEventAction>
            {
                new("securityReview", "Review Alert", receptionHref)
            },
            _ => new List<CameraFaceEventAction>
            {
                new("viewReception", "View Reception", receptionHref)
            }
        };
    }

    private static string GetTitle(CameraFrameFaceResultDto face)
    {
        if (face.IsBlacklisted)
        {
            return "Blacklisted Person Detected";
        }

        if (!face.IsKnown)
        {
            return "Unknown Person Detected";
        }

        return face.PersonType.Equals("Staff", StringComparison.OrdinalIgnoreCase)
            ? "Staff Member Recognized"
            : "Known Visitor Recognized";
    }

    private static string GetMessage(
        CameraFrameRecognitionResultDto result,
        CameraFrameFaceResultDto face)
    {
        var cameraName = string.IsNullOrWhiteSpace(result.CameraName) ? $"Camera {result.CameraId}" : result.CameraName;
        var location = string.IsNullOrWhiteSpace(result.CameraLocationName)
            ? cameraName
            : $"{cameraName} / {result.CameraLocationName}";

        if (face.IsBlacklisted)
        {
            var person = string.IsNullOrWhiteSpace(face.FullName) ? "A blacklisted person" : face.FullName;
            return $"{person} was detected at {location}. Security review is required.";
        }

        if (!face.IsKnown)
        {
            return $"An unknown person was detected at {location}. Receptionist review is required.";
        }

        var name = string.IsNullOrWhiteSpace(face.FullName) ? face.PersonType : face.FullName;
        var action = GetSuggestedActionLabel(face.SuggestedAction);
        return $"{name} was recognized at {location}. Suggested action: {action}.";
    }

    private static string GetSuggestedActionLabel(string? action)
    {
        return action switch
        {
            "confirmCheckIn" => "Confirm check-in",
            "confirmCheckOut" => "Confirm check-out",
            "autoCheckIn" => "Automatic check-in",
            "autoCheckOut" => "Automatic check-out",
            "reviewUnknown" or "reviewUnknownFaces" => "Review unknown person",
            "securityAlert" => "Security review",
            "reviewKnown" or "reviewKnownFaces" => "Review known person",
            _ => "Review"
        };
    }

    private static NotificationAlertType GetNotificationAlertType(CameraFrameFaceResultDto face)
    {
        if (face.IsBlacklisted)
        {
            return NotificationAlertType.BlacklistAlert;
        }

        if (!face.IsKnown)
        {
            return NotificationAlertType.UnknownFace;
        }

        if (face.IsVip)
        {
            return NotificationAlertType.VipArrival;
        }

        return face.PersonType.Equals("Visitor", StringComparison.OrdinalIgnoreCase)
            ? NotificationAlertType.VisitorArrival
            : NotificationAlertType.Custom;
    }

    private static AlertPriority GetPriority(CameraFrameFaceResultDto face)
    {
        if (face.IsBlacklisted)
        {
            return AlertPriority.Critical;
        }

        if (face.IsVip)
        {
            return AlertPriority.High;
        }

        return face.IsKnown ? AlertPriority.Medium : AlertPriority.Medium;
    }

    private static string GetEventType(CameraFrameFaceResultDto face)
    {
        if (face.IsBlacklisted)
        {
            return "blacklistedFaceDetected";
        }

        return face.IsKnown ? "knownFaceDetected" : "unknownFaceDetected";
    }

    private static string GetModalAlertType(CameraFrameFaceResultDto face)
    {
        if (face.IsBlacklisted)
        {
            return "blacklist";
        }

        if (face.IsVip)
        {
            return "vip";
        }

        return face.IsKnown ? "known_face" : "unknown_face";
    }

    private string BuildIdentityKey(
        CameraFrameRecognitionResultDto result,
        CameraFrameFaceResultDto face,
        CameraConfiguration config,
        DateTime now)
    {
        var personKey = face.IsKnown
            ? $"{face.PersonType}:{face.PersonId?.ToString() ?? face.SubjectId ?? face.FullName ?? face.Index.ToString()}"
            : $"unknown:{ResolveUnknownTrackId(result, face, config, now)}";

        return $"{result.CameraId}:{personKey}".ToLowerInvariant();
    }

    private string ResolveUnknownTrackId(
        CameraFrameRecognitionResultDto result,
        CameraFrameFaceResultDto face,
        CameraConfiguration config,
        DateTime now)
    {
        if (!string.IsNullOrWhiteSpace(result.ClientTrackId))
        {
            return $"client:{result.ClientTrackId}";
        }

        if (face.BoundingBox == null)
        {
            return $"source:{result.SourceDeviceId ?? "source"}:face:{face.Index}";
        }

        var trackWindow = GetTrackWindow(config);
        var cacheKey = $"camera-face:unknown-tracks:{result.CameraId}:{result.SourceDeviceId ?? "source"}";
        var tracks = _memoryCache.Get<List<UnknownFaceTrackState>>(cacheKey) ?? new List<UnknownFaceTrackState>();

        tracks.RemoveAll(track => track.LastSeenAt.Add(trackWindow) <= now);

        UnknownFaceTrackState? bestTrack = null;
        var bestScore = 0d;

        foreach (var track in tracks)
        {
            var score = CalculateUnknownTrackSimilarity(track.LastBox, face.BoundingBox);
            if (score > bestScore)
            {
                bestScore = score;
                bestTrack = track;
            }
        }

        if (bestTrack != null && bestScore >= UnknownTrackIouThreshold)
        {
            bestTrack.LastBox = CopyBox(face.BoundingBox);
            bestTrack.LastSeenAt = now;
            _memoryCache.Set(cacheKey, tracks, trackWindow);
            return bestTrack.TrackId;
        }

        var newTrack = new UnknownFaceTrackState(
            Guid.NewGuid().ToString("N"),
            CopyBox(face.BoundingBox),
            now);

        tracks.Add(newTrack);
        _memoryCache.Set(cacheKey, tracks, trackWindow);

        return newTrack.TrackId;
    }

    private static double CalculateUnknownTrackSimilarity(
        CameraFrameFaceBoxDto first,
        CameraFrameFaceBoxDto second)
    {
        var iou = CalculateIntersectionOverUnion(first, second);
        if (iou >= UnknownTrackIouThreshold)
        {
            return iou;
        }

        var firstCenterX = first.X + first.Width / 2d;
        var firstCenterY = first.Y + first.Height / 2d;
        var secondCenterX = second.X + second.Width / 2d;
        var secondCenterY = second.Y + second.Height / 2d;
        var centerDistance = Math.Sqrt(
            Math.Pow(firstCenterX - secondCenterX, 2) +
            Math.Pow(firstCenterY - secondCenterY, 2));

        var firstArea = Math.Max(1d, first.Width * first.Height);
        var secondArea = Math.Max(1d, second.Width * second.Height);
        var sizeRatio = Math.Min(firstArea, secondArea) / Math.Max(firstArea, secondArea);
        if (sizeRatio < 0.35d)
        {
            return iou;
        }

        var averageFaceSize = Math.Max(
            1d,
            (Math.Max(first.Width, first.Height) + Math.Max(second.Width, second.Height)) / 2d);
        var centerScore = Math.Max(0d, 1d - centerDistance / (averageFaceSize * 1.5d));

        return Math.Max(iou, centerScore * sizeRatio);
    }

    private static CameraFrameFaceBoxDto CopyBox(CameraFrameFaceBoxDto box)
    {
        return new CameraFrameFaceBoxDto
        {
            X = box.X,
            Y = box.Y,
            Width = box.Width,
            Height = box.Height,
            Confidence = box.Confidence
        };
    }

    private static double CalculateIntersectionOverUnion(
        CameraFrameFaceBoxDto first,
        CameraFrameFaceBoxDto second)
    {
        var firstRight = first.X + first.Width;
        var firstBottom = first.Y + first.Height;
        var secondRight = second.X + second.Width;
        var secondBottom = second.Y + second.Height;

        var intersectionLeft = Math.Max(first.X, second.X);
        var intersectionTop = Math.Max(first.Y, second.Y);
        var intersectionRight = Math.Min(firstRight, secondRight);
        var intersectionBottom = Math.Min(firstBottom, secondBottom);

        var intersectionWidth = Math.Max(0, intersectionRight - intersectionLeft);
        var intersectionHeight = Math.Max(0, intersectionBottom - intersectionTop);
        var intersectionArea = intersectionWidth * intersectionHeight;

        if (intersectionArea <= 0)
        {
            return 0d;
        }

        var firstArea = Math.Max(0, first.Width) * Math.Max(0, first.Height);
        var secondArea = Math.Max(0, second.Width) * Math.Max(0, second.Height);
        var unionArea = firstArea + secondArea - intersectionArea;

        return unionArea <= 0 ? 0d : (double)intersectionArea / unionArea;
    }

    private static TimeSpan GetTrackWindow(CameraConfiguration config)
    {
        var milliseconds = Math.Max(1000, Math.Max(config.TrackTimeoutMs, config.RecognitionIntervalPerTrackMs));
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static TimeSpan GetEventCooldown(CameraConfiguration config, CameraFrameFaceResultDto face)
    {
        var faceCooldown = face.IsBlacklisted
            ? config.AlertCooldownMs
            : face.IsKnown
                ? config.KnownFaceCooldownMs
                : config.UnknownFaceCooldownMs;

        var milliseconds = Math.Max(config.AlertCooldownMs, faceCooldown);
        milliseconds = Math.Max(milliseconds, config.RecognitionIntervalPerTrackMs);
        return TimeSpan.FromMilliseconds(Math.Clamp(milliseconds, 1000, 86_400_000));
    }

    private async Task<CameraFaceEvent?> CreateFaceEventRecordAsync(
        CameraFrameRecognitionResultDto result,
        CameraFrameFaceResultDto face,
        string eventId,
        byte[]? frameBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            string? snapshotPath = null;
            if (frameBytes is { Length: > 0 } && face.BoundingBox != null)
            {
                var category = face.IsKnown ? "known" : "unknown";
                snapshotPath = await _snapshotService.SaveFaceCropAsync(
                    frameBytes,
                    face.BoundingBox.X, face.BoundingBox.Y,
                    face.BoundingBox.Width, face.BoundingBox.Height,
                    result.CameraId, category, cancellationToken);
            }

            bool isCandidate = false;
            if (face.IsKnown && face.PersonId.HasValue && snapshotPath != null)
            {
                var candidateCount = await _unitOfWork.FaceEvents.GetKnownCandidateCountAsync(
                    face.PersonId.Value, face.PersonType, cancellationToken);

                if (candidateCount >= KnownCandidateMaxSnapshots)
                {
                    // FIFO eviction: demote oldest candidate so the new one takes its slot.
                    var oldest = await _unitOfWork.FaceEvents.GetOldestKnownCandidateAsync(
                        face.PersonId.Value, face.PersonType, cancellationToken);
                    if (oldest != null)
                    {
                        oldest.IsKnownCandidate = false;
                    }
                }

                isCandidate = true;
            }

            var faceEvent = new CameraFaceEvent
            {
                EventId = eventId,
                CameraId = result.CameraId,
                CameraName = result.CameraName,
                LocationId = result.CameraLocationId,
                CapturedAt = result.CapturedAt,
                IsKnown = face.IsKnown,
                PersonType = face.PersonType,
                PersonId = face.PersonId,
                SubjectId = face.SubjectId,
                Similarity = face.Similarity > 0 ? (float?)face.Similarity : null,
                Confidence = face.Confidence > 0 ? (float?)face.Confidence : null,
                SnapshotPath = snapshotPath,
                BoundingBoxX = face.BoundingBox?.X,
                BoundingBoxY = face.BoundingBox?.Y,
                BoundingBoxWidth = face.BoundingBox?.Width,
                BoundingBoxHeight = face.BoundingBox?.Height,
                SuggestedAction = face.SuggestedAction ?? "none",
                ReviewStatus = FaceEventReviewStatus.Pending,
                IsKnownCandidate = isCandidate,
                ExpiresAt = face.IsKnown ? null : DateTime.UtcNow.AddDays(UnknownRetentionDays)
            };

            await _unitOfWork.FaceEvents.AddAsync(faceEvent, cancellationToken);
            return faceEvent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create durable face event record for camera {CameraId}", result.CameraId);
            return null;
        }
    }

    private sealed class FaceActiveState
    {
        public FaceActiveState(DateTime lastSeenAt)
        {
            LastSeenAt = lastSeenAt;
        }

        public DateTime LastSeenAt { get; set; }
    };

    private sealed class UnknownFaceTrackState
    {
        public UnknownFaceTrackState(string trackId, CameraFrameFaceBoxDto lastBox, DateTime lastSeenAt)
        {
            TrackId = trackId;
            LastBox = lastBox;
            LastSeenAt = lastSeenAt;
        }

        public string TrackId { get; }
        public CameraFrameFaceBoxDto LastBox { get; set; }
        public DateTime LastSeenAt { get; set; }
    }

    private sealed record CooldownDecision(
        bool ShouldEmit,
        string? Reason,
        DateTime? NextAllowedAt,
        TimeSpan Cooldown);

    private sealed class CameraFaceEventPayload
    {
        public string EventId { get; set; } = string.Empty;
        public int? AlertId { get; set; }
        public int? FaceEventDbId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public bool RequiresAcknowledgement { get; set; }
        public string SuggestedAction { get; set; } = "none";
        public string SuggestedActionLabel { get; set; } = string.Empty;
        public List<CameraFaceEventAction> Actions { get; set; } = new();
        public int CameraId { get; set; }
        public string CameraName { get; set; } = string.Empty;
        public int? CameraLocationId { get; set; }
        public string? CameraLocationName { get; set; }
        public string CameraType { get; set; } = string.Empty;
        public string CameraRole { get; set; } = string.Empty;
        public string WorkflowMode { get; set; } = string.Empty;
        public DateTime CapturedAt { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string EngineUsed { get; set; } = string.Empty;
        public bool FallbackUsed { get; set; }
        public string? SourceDeviceId { get; set; }
        public string? ClientTrackId { get; set; }
        public CameraFrameFaceResultDto Face { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    private sealed record CameraFaceEventAction(string Action, string Label, string Href);
}
