using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Per-camera buffer that collects inference results over a fixed capture window per tracked face,
/// then emits the highest-scoring result when the window closes or the track is lost.
///
/// Purpose: instead of emitting on the first inference hit (which may be blurry or off-angle),
/// the buffer samples multiple frames during the window and keeps the sharpest, most frontal one.
/// </summary>
internal sealed class BestFaceEmissionBuffer
{
    private readonly int _captureDurationMs;
    private readonly double _minImprovement;
    private readonly Dictionary<long, Candidate> _candidates = new();
    private readonly object _lock = new();
    private readonly ILogger? _logger;
    private readonly int _cameraId;

    public BestFaceEmissionBuffer(int captureDurationMs, double minImprovement, int cameraId = 0, ILogger? logger = null)
    {
        _captureDurationMs = captureDurationMs;
        _minImprovement = minImprovement;
        _cameraId = cameraId;
        _logger = logger;
    }

    /// <summary>
    /// Returns true when faceId is within its active capture window.
    /// Called by ShouldRunInferenceForTracks to bypass RecognitionIntervalPerTrackMs
    /// during the window so multiple quality samples are collected.
    /// </summary>
    public bool IsInCaptureWindow(long faceId)
    {
        lock (_lock)
        {
            return _captureDurationMs > 0 &&
                   _candidates.TryGetValue(faceId, out var c) &&
                   (DateTime.UtcNow - c.FirstSeenAt).TotalMilliseconds < _captureDurationMs;
        }
    }

    /// <summary>
    /// Feeds a new inference result into the buffer.
    /// Returns candidates whose capture window has elapsed and are ready to emit.
    /// </summary>
    public IReadOnlyList<Candidate> Update(
        CameraFrameRecognitionResultDto result,
        IReadOnlyList<TrackedFace> trackedFaces)
    {
        var ready = new List<Candidate>();
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            _logger?.LogDebug(
                "BestFaceBuffer cam {CameraId}: Update() — {FaceCount} faces in result, {TrackerCount} tracker faces, {CandidateCount} buffered candidates",
                _cameraId, result.Faces.Count, trackedFaces.Count, _candidates.Count);

            foreach (var face in result.Faces)
            {
                var (faceId, bestIou) = MatchToTrackerFaceId(face.BoundingBox, trackedFaces);

                if (faceId == null)
                {
                    _logger?.LogWarning(
                        "BestFaceBuffer cam {CameraId}: Detection box ({DX},{DY} {DW}×{DH} Conf={Conf:F2}) — best IoU={BestIou:F3} against {Count} tracker face(s): [{Trackers}] — face DROPPED.",
                        _cameraId,
                        face.BoundingBox?.X, face.BoundingBox?.Y, face.BoundingBox?.Width, face.BoundingBox?.Height,
                        face.Confidence, bestIou, trackedFaces.Count,
                        string.Join("; ", trackedFaces.Select(t => $"id={t.FaceId}({t.X},{t.Y} {t.Width}×{t.Height})")));
                    continue;
                }

                _logger?.LogDebug(
                    "BestFaceBuffer cam {CameraId}: detection box (X={X} Y={Y} W={W} H={H}) matched tracker ID={FaceId} IoU={IoU:F3} — buffered",
                    _cameraId,
                    face.BoundingBox?.X, face.BoundingBox?.Y,
                    face.BoundingBox?.Width, face.BoundingBox?.Height,
                    faceId, bestIou);

                var score = ComputeScore(face);
                var singleFaceResult = CloneResultForFace(result, face);

                _logger?.LogWarning(
                    "BestFaceBuffer cam {CameraId}: FaceId={FaceId} score={Score:F1} (quality={Quality:F1} sharpness={Sharpness:F1} roll={Roll:F1})",
                    _cameraId, faceId,
                    score,
                    face.DetectionQualityScore ?? (face.Confidence * 100.0),
                    face.SharpnessScore ?? 80.0,
                    face.DetectionRoll.HasValue ? Math.Abs(face.DetectionRoll.Value) : 0f);

                if (!_candidates.TryGetValue(faceId.Value, out var existing))
                {
                    // Start the capture window. Do NOT emit yet — wait for the window to close
                    // or the track to be lost, then emit the highest-scoring frame seen.
                    _candidates[faceId.Value] = new Candidate(faceId.Value, singleFaceResult, face, score, now, AlreadyEmitted: false);
                    _logger?.LogWarning("BestFaceBuffer cam {CameraId}: FaceId={FaceId} window started — first candidate score={Score:F1}", _cameraId, faceId, score);
                    continue;
                }

                // Replace if the new frame scores meaningfully better.
                if (score > existing.CompositeScore * (1.0 + _minImprovement))
                {
                    _logger?.LogWarning(
                        "BestFaceBuffer cam {CameraId}: FaceId={FaceId} candidate REPLACED — old={OldScore:F1} new={NewScore:F1} (+{Pct:F1}%)",
                        _cameraId, faceId, existing.CompositeScore, score,
                        (score / existing.CompositeScore - 1.0) * 100.0);
                    _candidates[faceId.Value] = existing with
                    {
                        Result = singleFaceResult,
                        Face = face,
                        CompositeScore = score
                    };
                }
                else
                {
                    _logger?.LogWarning(
                        "BestFaceBuffer cam {CameraId}: FaceId={FaceId} candidate kept — existing={ExistingScore:F1} new={NewScore:F1} (threshold={Threshold:F1})",
                        _cameraId, faceId, existing.CompositeScore, score,
                        existing.CompositeScore * (1.0 + _minImprovement));
                }

                // Emit the best candidate when the capture window elapses.
                if (_captureDurationMs > 0 &&
                    (now - existing.FirstSeenAt).TotalMilliseconds >= _captureDurationMs)
                {
                    var toEmit = _candidates[faceId.Value];
                    _logger?.LogWarning(
                        "BestFaceBuffer cam {CameraId}: FaceId={FaceId} window closed ({Ms:F0}ms) — emitting best score={Score:F1}",
                        _cameraId, faceId, (now - existing.FirstSeenAt).TotalMilliseconds, toEmit.CompositeScore);
                    ready.Add(toEmit);
                    _candidates.Remove(faceId.Value);
                }
            }
        }

        return ready;
    }

    /// <summary>
    /// Emits any candidates whose tracker IDs are no longer active (track lost).
    /// Guarantees no face is silently dropped even for fast walkthroughs shorter than the window.
    /// </summary>
    public IReadOnlyList<Candidate> FlushLostTracks(IReadOnlyCollection<long> activeIds)
    {
        var flushed = new List<Candidate>();

        lock (_lock)
        {
            foreach (var id in _candidates.Keys.ToList())
            {
                if (!activeIds.Contains(id))
                {
                    var candidate = _candidates[id];
                    _candidates.Remove(id);
                    if (!candidate.AlreadyEmitted)
                    {
                        _logger?.LogWarning(
                            "BestFaceBuffer cam {CameraId}: FaceId={FaceId} track lost — flushing best score={Score:F1}",
                            _cameraId, id, candidate.CompositeScore);
                        flushed.Add(candidate);
                    }
                }
            }
        }

        return flushed;
    }

    /// <summary>
    /// Composite quality score for one face result.
    /// Higher = better candidate for the best-frame snapshot.
    ///
    /// Weights:
    ///   55% detection quality (Luxand size+centering score, 0–100)
    ///   35% sharpness (Sobel edge score, 0–100; defaults to 80 when unknown)
    ///   10% roll bonus (100 at 0°, falls to 0 at 50° tilt)
    /// </summary>
    private static double ComputeScore(CameraFrameFaceResultDto face)
    {
        var quality   = face.DetectionQualityScore ?? (face.Confidence * 100.0);
        var sharpness = face.SharpnessScore ?? 80.0;
        var roll      = face.DetectionRoll.HasValue ? Math.Abs(face.DetectionRoll.Value) : 0f;
        var rollBonus = Math.Max(0.0, 100.0 - roll * 2.0);

        return quality * 0.55 + sharpness * 0.35 + rollBonus * 0.10;
    }

    /// <summary>
    /// Finds the tracker face ID closest to the recognized face bounding box using IoU matching.
    /// Returns (null, bestIou) when no tracker face overlaps with IoU >= 0.30.
    /// bestIou is the highest IoU found (even if below threshold) for diagnostic logging.
    /// </summary>
    private static (long? FaceId, double BestIou) MatchToTrackerFaceId(
        CameraFrameFaceBoxDto? box,
        IReadOnlyList<TrackedFace> trackedFaces)
    {
        if (box == null || trackedFaces.Count == 0)
        {
            return (null, 0.0);
        }

        const double minIou = 0.30;
        var bestIou   = 0.0;
        long? bestId  = null;

        foreach (var tracked in trackedFaces)
        {
            var iou = ComputeIoU(
                box.X, box.Y, box.Width, box.Height,
                tracked.X, tracked.Y, tracked.Width, tracked.Height);

            if (iou > bestIou)
            {
                bestIou = iou;
                if (iou >= minIou)
                    bestId = tracked.FaceId;
            }
        }

        return (bestId, bestIou);
    }

    private static double ComputeIoU(
        int ax, int ay, int aw, int ah,
        int bx, int by, int bw, int bh)
    {
        var interX1 = Math.Max(ax, bx);
        var interY1 = Math.Max(ay, by);
        var interX2 = Math.Min(ax + aw, bx + bw);
        var interY2 = Math.Min(ay + ah, by + bh);

        if (interX2 <= interX1 || interY2 <= interY1)
        {
            return 0.0;
        }

        var interArea = (double)(interX2 - interX1) * (interY2 - interY1);
        var aArea = (double)aw * ah;
        var bArea = (double)bw * bh;
        var unionArea = aArea + bArea - interArea;

        return unionArea <= 0 ? 0.0 : interArea / unionArea;
    }

    /// <summary>
    /// Produces a copy of the full frame result containing only the specified face.
    /// The crop in CameraFaceEventService uses the bounding box from result.Faces[0],
    /// so isolating the face here ensures the right box is used for the snapshot.
    /// FrameBytes is shared by reference — it is read-only after production.
    /// </summary>
    private static CameraFrameRecognitionResultDto CloneResultForFace(
        CameraFrameRecognitionResultDto source,
        CameraFrameFaceResultDto face)
    {
        return new CameraFrameRecognitionResultDto
        {
            Success = source.Success,
            CameraId = source.CameraId,
            CameraName = source.CameraName,
            CameraLocationId = source.CameraLocationId,
            CameraLocationName = source.CameraLocationName,
            CameraType = source.CameraType,
            CameraRole = source.CameraRole,
            WorkflowMode = source.WorkflowMode,
            PreferredEngine = source.PreferredEngine,
            EngineUsed = source.EngineUsed,
            FallbackUsed = source.FallbackUsed,
            RecognitionSkipped = source.RecognitionSkipped,
            SkipReason = source.SkipReason,
            EngineStatus = source.EngineStatus,
            CapturedAt = source.CapturedAt,
            ProcessedAt = source.ProcessedAt,
            FrameSizeBytes = source.FrameSizeBytes,
            SourceDeviceId = source.SourceDeviceId,
            ClientTrackId = source.ClientTrackId,
            DetectedFaceCount = 1,
            RecognizedFaceCount = face.IsKnown ? 1 : 0,
            KnownFaceCount = face.IsKnown ? 1 : 0,
            UnknownFaceCount = face.IsKnown ? 0 : 1,
            RecognitionThreshold = source.RecognitionThreshold,
            DetectionThreshold = source.DetectionThreshold,
            SuggestedWorkflowAction = face.SuggestedAction,
            Faces = [face],
            Events = [],
            FrameBytes = source.FrameBytes   // shared reference — read-only after creation
        };
    }
}

/// <summary>Best-frame candidate held in the buffer for one tracked face.</summary>
internal sealed record Candidate(
    long FaceId,
    CameraFrameRecognitionResultDto Result,
    CameraFrameFaceResultDto Face,
    double CompositeScore,
    DateTime FirstSeenAt,
    bool AlreadyEmitted = false);
