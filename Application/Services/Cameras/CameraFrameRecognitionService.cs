using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Application.Services.Common;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace VisitorManagementSystem.Api.Application.Services.Cameras;

/// <summary>
/// Processes individual camera frames through the configured recognition engine.
/// Luxand is the primary engine for hybrid/Luxand cameras, with CompreFace fallback
/// when enabled by the camera configuration.
/// </summary>
public class CameraFrameRecognitionService : ICameraFrameRecognitionService
{
    private const long DefaultMaxFrameBytes = 5 * 1024 * 1024;
    private const double BoundingBoxMatchThreshold = 0.25;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IFaceDetectionService _luxandService;
    private readonly IFaceDetectionService _compreFaceService;
    private readonly IUrlResolverService _urlResolver;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CameraFrameRecognitionService> _logger;

    public CameraFrameRecognitionService(
        IUnitOfWork unitOfWork,
        [Microsoft.Extensions.DependencyInjection.FromKeyedServices("luxand")] IFaceDetectionService luxandService,
        [Microsoft.Extensions.DependencyInjection.FromKeyedServices("compreface")] IFaceDetectionService compreFaceService,
        IUrlResolverService urlResolver,
        IConfiguration configuration,
        ILogger<CameraFrameRecognitionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _luxandService = luxandService ?? throw new ArgumentNullException(nameof(luxandService));
        _compreFaceService = compreFaceService ?? throw new ArgumentNullException(nameof(compreFaceService));
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CameraFrameRecognitionResultDto> ProcessFrameAsync(
        int cameraId,
        CameraFrameProcessingRequestDto request,
        int? processedByUserId = null,
        string? clientIpAddress = null,
        CancellationToken cancellationToken = default)
    {
        var camera = await _unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);
        if (camera == null)
        {
            return CameraFrameRecognitionResultDto.Failure(cameraId, "CameraNotFound", "Camera not found");
        }

        var config = camera.GetConfiguration();
        var capturedAt = request.CapturedAt?.ToUniversalTime() ?? DateTime.UtcNow;
        var result = CreateBaseResult(camera, config, request, capturedAt);

        if (camera.IsDeleted || !camera.IsActive || !config.Enabled)
        {
            result.Success = false;
            result.ErrorCode = "CameraDisabled";
            result.ErrorMessage = "Camera is disabled or inactive";
            return result;
        }

        if (!camera.EnableFacialRecognition)
        {
            MarkSkipped(result, "Facial recognition is disabled for this camera");
            return result;
        }

        if (request.Frame == null || request.Frame.Length == 0)
        {
            result.Success = false;
            result.ErrorCode = "EmptyFrame";
            result.ErrorMessage = "A non-empty frame image is required";
            return result;
        }

        if (!IsSupportedImage(request.Frame))
        {
            result.Success = false;
            result.ErrorCode = "UnsupportedFrame";
            result.ErrorMessage = "Frame must be a JPEG, PNG, or WebP image";
            return result;
        }

        var maxFrameBytes = GetMaxFrameBytes();
        if (request.Frame.Length > maxFrameBytes)
        {
            result.Success = false;
            result.ErrorCode = "FrameTooLarge";
            result.ErrorMessage = $"Frame size exceeds the configured limit of {maxFrameBytes / (1024 * 1024)} MB";
            return result;
        }

        var frameBytes = await ReadFrameAsync(request.Frame, cancellationToken);
        result.FrameSizeBytes = frameBytes.Length;
        result.FrameBytes = frameBytes;

        if (!IsFrameUsable(frameBytes))
        {
            MarkSkipped(result, "Frame rejected: too dark or uniform (stream artifact)");
            return result;
        }

        var maxFaces = Math.Max(1, config.MaxFacesPerFrame);

        // For Luxand/Hybrid cameras: recognition internally runs detection+extraction+matching in a
        // single SDK image load, so we skip a separate DetectFacesAsync call.
        // The recognition result contains both matched faces (SubjectId set) and detected-unknown
        // faces (SubjectId empty) — one pass covers both.
        // For CompreFace-only cameras we keep the original two-pass approach because CompreFace
        // recognition does not return unmatched detected faces.
        var useSinglePass = config.RecognitionEnabled &&
                            config.PreferredEngine != FaceRecognitionEngine.CompreFace;

        if (useSinglePass)
        {
            var recognitionResult = await RecognizeFacesAsync(frameBytes, config, cancellationToken);
            var allFaces = recognitionResult.Faces;

            // Faces with a SubjectId were matched; those without were detected but not recognized.
            var matchedFaces = allFaces.Where(f => !string.IsNullOrEmpty(f.SubjectId)).ToList();
            // Keep the full RecognizedFace so bestSimilarity (partial match score) is preserved on the result DTO.
            var unmatchedDetected = allFaces
                .Where(f => string.IsNullOrEmpty(f.SubjectId) && f.BoundingBox != null)
                .ToList();

            result.DetectedFaceCount = allFaces.Count(f => f.BoundingBox != null);
            result.RecognizedFaceCount = matchedFaces.Count;
            result.EngineUsed = recognitionResult.EngineUsed ?? "none";
            result.FallbackUsed = recognitionResult.FallbackUsed;

            var faces = new List<CameraFrameFaceResultDto>();
            foreach (var recognizedFace in matchedFaces.OrderByDescending(f => f.Similarity).Take(maxFaces))
            {
                faces.Add(await BuildRecognizedFaceAsync(recognizedFace, config, cancellationToken));
            }

            foreach (var unknownFace in BuildUnmatchedUnknownFaces(unmatchedDetected, faces, maxFaces - faces.Count))
            {
                faces.Add(unknownFace);
            }

            result.Faces = faces
                .OrderByDescending(f => f.IsKnown)
                .ThenByDescending(f => f.Similarity)
                .ThenByDescending(f => f.Confidence)
                .Take(maxFaces)
                .Select((face, index) => { face.Index = index; return face; })
                .ToList();

            AttachFaceSnapshots(result.Faces, frameBytes, config);
            result.KnownFaceCount = result.Faces.Count(f => f.IsKnown);
            result.UnknownFaceCount = result.Faces.Count(f => !f.IsKnown);
            result.SuggestedWorkflowAction = GetFrameWorkflowAction(result.Faces, config);
            result.Success = true;
            result.ProcessedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Processed camera frame (single-pass). CameraId={CameraId}, Type={CameraType}, Role={CameraRole}, Engine={Engine}, Detected={Detected}, Known={KnownCount}, Unknown={UnknownCount}, ClientIp={ClientIp}",
                camera.Id, camera.CameraType, config.CameraRole, result.EngineUsed,
                result.DetectedFaceCount, result.KnownFaceCount, result.UnknownFaceCount, clientIpAddress);

            return result;
        }

        // Two-pass path: CompreFace cameras or detection-only mode.
        var detectedFaces = new List<DetectedFace>();
        EngineCallResult<DetectedFace>? detectionResult = null;

        if (config.DetectionEnabled)
        {
            detectionResult = await DetectFacesAsync(frameBytes, config, cancellationToken);
            detectedFaces = detectionResult.Faces;
            result.DetectedFaceCount = detectedFaces.Count;
        }

        if (!config.RecognitionEnabled)
        {
            result.EngineUsed = detectionResult?.EngineUsed ?? "none";
            result.FallbackUsed = detectionResult?.FallbackUsed ?? false;
            result.Faces = BuildUnknownFaces(detectedFaces, maxFaces);
            AttachFaceSnapshots(result.Faces, frameBytes, config);
            result.UnknownFaceCount = result.Faces.Count;
            result.Success = true;
            MarkSkipped(result, "Recognition is disabled for this camera");
            return result;
        }

        var twoPassRecognitionResult = await RecognizeFacesAsync(frameBytes, config, cancellationToken);
        var recognizedFaces = twoPassRecognitionResult.Faces.Where(f => !string.IsNullOrEmpty(f.SubjectId)).ToList();
        result.RecognizedFaceCount = recognizedFaces.Count;
        result.EngineUsed = twoPassRecognitionResult.EngineUsed ?? detectionResult?.EngineUsed ?? "none";
        result.FallbackUsed = twoPassRecognitionResult.FallbackUsed || (detectionResult?.FallbackUsed ?? false);

        var twoPassFaces = new List<CameraFrameFaceResultDto>();
        foreach (var recognizedFace in recognizedFaces.OrderByDescending(f => f.Similarity).Take(maxFaces))
        {
            twoPassFaces.Add(await BuildRecognizedFaceAsync(recognizedFace, config, cancellationToken));
        }

        foreach (var unknownFace in BuildUnmatchedUnknownFaces(detectedFaces, twoPassFaces, maxFaces - twoPassFaces.Count))
        {
            twoPassFaces.Add(unknownFace);
        }

        result.Faces = twoPassFaces
            .OrderByDescending(f => f.IsKnown)
            .ThenByDescending(f => f.Similarity)
            .ThenByDescending(f => f.Confidence)
            .Take(maxFaces)
            .Select((face, index) => { face.Index = index; return face; })
            .ToList();

        AttachFaceSnapshots(result.Faces, frameBytes, config);

        result.KnownFaceCount = result.Faces.Count(f => f.IsKnown);
        result.UnknownFaceCount = result.Faces.Count(f => !f.IsKnown);
        result.SuggestedWorkflowAction = GetFrameWorkflowAction(result.Faces, config);
        result.Success = true;
        result.ProcessedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Processed camera frame. CameraId={CameraId}, Type={CameraType}, Role={CameraRole}, Engine={Engine}, Known={KnownCount}, Unknown={UnknownCount}, ClientIp={ClientIp}",
            camera.Id, camera.CameraType, config.CameraRole, result.EngineUsed,
            result.KnownFaceCount, result.UnknownFaceCount, clientIpAddress);

        return result;
    }

    public async Task<CameraDebugFrameDto> InspectFrameAsync(
        int cameraId,
        byte[] frameBytes,
        CancellationToken cancellationToken = default)
    {
        var camera = await _unitOfWork.Cameras.GetByIdAsync(cameraId, cancellationToken);
        var config = camera?.GetConfiguration() ?? Domain.ValueObjects.CameraConfiguration.Default;

        const int maxDisplayWidth = 960;
        byte[] displayBytes = frameBytes;
        int frameWidth = 0, frameHeight = 0;
        double scaleFactor = 1.0;

        try
        {
            using var img = Image.Load(frameBytes);
            frameWidth = img.Width;
            frameHeight = img.Height;

            if (img.Width > maxDisplayWidth)
            {
                scaleFactor = (double)maxDisplayWidth / img.Width;
                img.Mutate(x => x.Resize(maxDisplayWidth, 0));
                using var ms = new MemoryStream();
                await img.SaveAsJpegAsync(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 80 }, cancellationToken);
                displayBytes = ms.ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read or resize debug frame for camera {CameraId}", cameraId);
        }

        var result = new CameraDebugFrameDto
        {
            HasFrame = true,
            FrameSizeBytes = frameBytes.Length,
            FrameJpegBase64 = Convert.ToBase64String(displayBytes),
            FrameWidth = (int)(frameWidth * scaleFactor),
            FrameHeight = (int)(frameHeight * scaleFactor),
            EngineStatus = GetEngineStatus(),
            DetectionThreshold = GetThreshold(config.FaceDetectionThreshold),
            MinFaceSizePixels = config.MinimumFaceSizePixels ?? 0,
            MaxFaceSizePixels = config.MaximumFaceSizePixels,
            QualityThreshold = (config.FaceQualityThreshold ?? 0) / 100.0,
            BlurThreshold = config.BlurThreshold ?? 0,
            MaxFacesPerFrame = Math.Max(1, config.MaxFacesPerFrame)
        };

        // Run raw detection without any filters so every face is visible
        var engineOrder = GetEngineOrder(config);
        List<DetectedFace> rawFaces = [];
        string engineUsed = "none";

        foreach (var engine in engineOrder)
        {
            if (!engine.Service.IsAvailable)
            {
                continue;
            }

            try
            {
                await using var stream = new MemoryStream(frameBytes);
                rawFaces = await engine.Service.DetectFacesAsync(stream, cancellationToken);
                engineUsed = engine.Name;
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Debug inspection: {Engine} detection failed for camera {CameraId}", engine.Name, cameraId);
            }
        }

        result.EngineUsed = engineUsed;
        result.EngineAvailable = engineUsed != "none";
        result.RawDetectionCount = rawFaces.Count;

        var threshold = GetThreshold(config.FaceDetectionThreshold);
        var minSize = config.MinimumFaceSizePixels ?? 40;
        var maxSize = config.MaximumFaceSizePixels ?? 0;
        var qualityMin = config.FaceQualityThreshold ?? 20;
        var blurMin = config.BlurThreshold ?? 8;
        int passedCount = 0;

        foreach (var face in rawFaces)
        {
            var reasons = new List<string>();

            if (face.Confidence < threshold)
            {
                reasons.Add($"Confidence {face.Confidence * 100:F0}% < threshold {threshold * 100:F0}%");
            }

            if (minSize > 0 && face.Width < minSize && face.Height < minSize)
            {
                reasons.Add($"Size {Math.Max(face.Width, face.Height)}px < minimum {minSize}px");
            }

            if (maxSize > 0 && (face.Width > maxSize || face.Height > maxSize))
            {
                reasons.Add($"Size {Math.Max(face.Width, face.Height)}px > maximum {maxSize}px");
            }

            if (qualityMin > 0 && face.QualityScore.HasValue && face.QualityScore.Value < qualityMin)
            {
                reasons.Add($"Quality {face.QualityScore.Value:F0} < threshold {qualityMin}");
            }

            if (blurMin > 0)
            {
                var blurScore = ComputeBlurScore(frameBytes, face);
                if (blurScore < blurMin)
                {
                    reasons.Add($"Blur score {blurScore:F0} < threshold {blurMin}");
                }
            }

            var passed = reasons.Count == 0;
            if (passed)
            {
                passedCount++;
            }

            result.Faces.Add(new CameraDebugFaceDto
            {
                X = (int)(face.X * scaleFactor),
                Y = (int)(face.Y * scaleFactor),
                Width = (int)(face.Width * scaleFactor),
                Height = (int)(face.Height * scaleFactor),
                Confidence = face.Confidence,
                QualityScore = face.QualityScore,
                PassedFilter = passed,
                FilterReasons = reasons
            });
        }

        result.FilteredDetectionCount = passedCount;
        return result;
    }

    private CameraFrameRecognitionResultDto CreateBaseResult(
        Camera camera,
        Domain.ValueObjects.CameraConfiguration config,
        CameraFrameProcessingRequestDto request,
        DateTime capturedAt)
    {
        return new CameraFrameRecognitionResultDto
        {
            Success = false,
            CameraId = camera.Id,
            CameraName = camera.Name,
            CameraLocationId = camera.LocationId,
            CameraLocationName = camera.Location?.Name,
            CameraType = camera.CameraType,
            CameraRole = config.CameraRole,
            WorkflowMode = config.WorkflowMode,
            PreferredEngine = config.PreferredEngine,
            CapturedAt = capturedAt,
            ProcessedAt = DateTime.UtcNow,
            SourceDeviceId = request.SourceDeviceId,
            ClientTrackId = request.ClientTrackId,
            RecognitionThreshold = GetThreshold(config.FacialRecognitionThreshold),
            DetectionThreshold = GetThreshold(config.FaceDetectionThreshold),
            EngineStatus = GetEngineStatus()
        };
    }

    private Dictionary<string, object> GetEngineStatus()
    {
        return new Dictionary<string, object>
        {
            ["luxandAvailable"] = _luxandService.IsAvailable,
            ["luxandStatus"] = _luxandService.InitializationStatus,
            ["luxandReturnCode"] = _luxandService.LastReturnCode?.ToString() ?? string.Empty,
            ["luxandError"] = _luxandService.InitializationError ?? string.Empty,
            ["compreFaceAvailable"] = _compreFaceService.IsAvailable,
            // Diagnostic: confirm which values took effect at startup (requires server restart to update)
            ["luxandDetectionThreshold"] = _configuration.GetValue<int>("LuxandFaceSDK:DetectionThreshold", 5),
            ["luxandInternalResizeWidth"] = _configuration.GetValue<int>("LuxandFaceSDK:InternalResizeWidth", 384)
        };
    }

    private static void MarkSkipped(CameraFrameRecognitionResultDto result, string? reason)
    {
        result.Success = true;
        result.RecognitionSkipped = true;
        result.SkipReason = reason;
        result.ProcessedAt = DateTime.UtcNow;
    }

    private async Task<EngineCallResult<DetectedFace>> DetectFacesAsync(
        byte[] frameBytes,
        Domain.ValueObjects.CameraConfiguration config,
        CancellationToken cancellationToken)
    {
        var threshold = GetThreshold(config.FaceDetectionThreshold);
        var minimumFaceSize = config.MinimumFaceSizePixels ?? 40;
        var qualityThreshold = config.FaceQualityThreshold ?? 20;
        var maximumFaceSize = config.MaximumFaceSizePixels ?? 0;
        var blurThreshold = config.BlurThreshold ?? 8;
        var rollLimit = config.RollLimitDegrees ?? 0;

        foreach (var engine in GetEngineOrder(config))
        {
            if (!engine.Service.IsAvailable)
            {
                continue;
            }

            try
            {
                await using var stream = new MemoryStream(frameBytes);
                var detectedFaces = await engine.Service.DetectFacesAsync(stream, cancellationToken);

                // Decode the frame once for blur scoring rather than once per detected face.
                Image<Rgb24>? blurImage = blurThreshold > 0
                    ? TryLoadRgb24(frameBytes)
                    : null;

                List<DetectedFace> faces;
                try
                {
                    faces = detectedFaces
                        .Where(face => face.Confidence >= threshold)
                        .Where(face => minimumFaceSize <= 0 || face.Width >= minimumFaceSize || face.Height >= minimumFaceSize)
                        .Where(face => maximumFaceSize <= 0 || face.Width <= maximumFaceSize || face.Height <= maximumFaceSize)
                        .Where(face => qualityThreshold <= 0 || !face.QualityScore.HasValue || face.QualityScore.Value >= qualityThreshold)
                        .Where(face => blurThreshold <= 0 || blurImage == null || ComputeBlurScoreFromDecodedImage(blurImage, face) >= blurThreshold)
                        .Where(face => rollLimit <= 0 || !face.Roll.HasValue || Math.Abs(face.Roll.Value) <= rollLimit)
                        .OrderByDescending(face => face.Confidence)
                        .Take(Math.Max(1, config.MaxFacesPerFrame))
                        .ToList();
                }
                finally
                {
                    blurImage?.Dispose();
                }

                return new EngineCallResult<DetectedFace>(faces, engine.Name, engine.IsFallback);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Engine} face detection failed while processing camera frame", engine.Name);
                if (!ShouldTryNextEngine(config, engine))
                {
                    return new EngineCallResult<DetectedFace>([], engine.Name, engine.IsFallback);
                }
            }
        }

        return new EngineCallResult<DetectedFace>([], null, false);
    }

    private async Task<EngineCallResult<RecognizedFace>> RecognizeFacesAsync(
        byte[] frameBytes,
        Domain.ValueObjects.CameraConfiguration config,
        CancellationToken cancellationToken)
    {
        var minimumFaceSize = config.MinimumFaceSizePixels ?? 40;
        var maximumFaceSize = config.MaximumFaceSizePixels ?? 0;
        var qualityThreshold = config.FaceQualityThreshold ?? 40;
        var blurThreshold = config.BlurThreshold ?? 8;
        // UnknownFaceThreshold is the lower bound for matched faces.
        // FacialRecognitionThreshold determines "known" vs "low confidence" and is applied later.
        // FaceDetectionThreshold gates unmatched (truly unknown) faces by BoundingBox confidence.
        var unknownThreshold = GetThreshold(config.UnknownFaceThreshold);
        var detectionThreshold = GetThreshold(config.FaceDetectionThreshold);

        foreach (var engine in GetEngineOrder(config))
        {
            if (!engine.Service.IsAvailable)
            {
                continue;
            }

            try
            {
                await using var stream = new MemoryStream(frameBytes);
                var recognizedFaces = await engine.Service.RecognizeFacesAsync(stream, cancellationToken);

                // Decode frame once for blur scoring if needed.
                Image<Rgb24>? blurImage = blurThreshold > 0
                    ? TryLoadRgb24(frameBytes)
                    : null;

                List<RecognizedFace> faces;
                try
                {
                    faces = recognizedFaces
                        .Where(face =>
                        {
                            // Matched faces must meet the unknown threshold.
                            if (!string.IsNullOrEmpty(face.SubjectId) && face.Similarity < unknownThreshold)
                                return false;

                            var box = face.BoundingBox;
                            if (box == null)
                                return !string.IsNullOrEmpty(face.SubjectId);

                            // Unmatched detected faces must meet FaceDetectionThreshold (BoundingBox confidence = quality/100).
                            if (string.IsNullOrEmpty(face.SubjectId) && detectionThreshold > 0 && box.Confidence < detectionThreshold)
                                return false;

                            if (minimumFaceSize > 0 && box.Width < minimumFaceSize && box.Height < minimumFaceSize)
                                return false;

                            if (maximumFaceSize > 0 && (box.Width > maximumFaceSize || box.Height > maximumFaceSize))
                                return false;

                            if (qualityThreshold > 0 && box.QualityScore.HasValue && box.QualityScore.Value < qualityThreshold)
                                return false;

                            if (blurThreshold > 0 && blurImage != null && ComputeBlurScoreFromDecodedImage(blurImage, box) < blurThreshold)
                                return false;

                            return true;
                        })
                        .OrderByDescending(face => face.Similarity)
                        .Take(Math.Max(1, config.MaxFacesPerFrame))
                        .ToList();
                }
                finally
                {
                    blurImage?.Dispose();
                }

                return new EngineCallResult<RecognizedFace>(faces, engine.Name, engine.IsFallback);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Engine} face recognition failed while processing camera frame", engine.Name);
                if (!ShouldTryNextEngine(config, engine))
                {
                    return new EngineCallResult<RecognizedFace>([], engine.Name, engine.IsFallback);
                }
            }
        }

        return new EngineCallResult<RecognizedFace>([], null, false);
    }

    private IReadOnlyList<EngineDescriptor> GetEngineOrder(Domain.ValueObjects.CameraConfiguration config)
    {
        return config.PreferredEngine switch
        {
            FaceRecognitionEngine.CompreFace => [new EngineDescriptor("CompreFace", _compreFaceService, false)],
            FaceRecognitionEngine.Luxand when config.CompreFaceFallbackEnabled =>
            [
                new EngineDescriptor("Luxand", _luxandService, false),
                new EngineDescriptor("CompreFace", _compreFaceService, true)
            ],
            FaceRecognitionEngine.Hybrid when config.CompreFaceFallbackEnabled =>
            [
                new EngineDescriptor("Luxand", _luxandService, false),
                new EngineDescriptor("CompreFace", _compreFaceService, true)
            ],
            _ => [new EngineDescriptor("Luxand", _luxandService, false)]
        };
    }

    private static bool ShouldTryNextEngine(
        Domain.ValueObjects.CameraConfiguration config,
        EngineDescriptor currentEngine)
    {
        return currentEngine.Name == "Luxand" &&
               config.CompreFaceFallbackEnabled &&
               (config.PreferredEngine == FaceRecognitionEngine.Luxand ||
                config.PreferredEngine == FaceRecognitionEngine.Hybrid);
    }

    private static string GetPrimaryEngineLabel(Domain.ValueObjects.CameraConfiguration config)
    {
        return config.PreferredEngine switch
        {
            FaceRecognitionEngine.CompreFace => "CompreFace",
            FaceRecognitionEngine.Hybrid => "Luxand",
            _ => "Luxand"
        };
    }

    private async Task<CameraFrameFaceResultDto> BuildRecognizedFaceAsync(
        RecognizedFace recognizedFace,
        Domain.ValueObjects.CameraConfiguration config,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(recognizedFace.SubjectId, cancellationToken);
        var recognitionThreshold = GetThreshold(config.FacialRecognitionThreshold);
        // Face must meet FacialRecognitionThreshold to be classified as "known".
        // Faces between UnknownFaceThreshold and FacialRecognitionThreshold are included
        // but classified as unknown (low-confidence matches).
        var isKnown = identity != null && recognizedFace.Similarity >= recognitionThreshold;

        return new CameraFrameFaceResultDto
        {
            IsKnown = isKnown,
            PersonType = identity?.PersonType ?? "Unknown",
            PersonId = identity?.PersonId,
            SubjectId = recognizedFace.SubjectId,
            FullName = identity?.FullName,
            Email = identity?.Email,
            EmployeeId = identity?.EmployeeId,
            ProfilePhotoUrl = identity?.ProfilePhotoUrl,
            Similarity = recognizedFace.Similarity,
            Confidence = recognizedFace.Confidence,
            BoundingBox = MapBox(recognizedFace.BoundingBox),
            IsVip = identity?.IsVip ?? false,
            IsBlacklisted = identity?.IsBlacklisted ?? false,
            BlacklistReason = identity?.BlacklistReason,
            SuggestedAction = GetFaceWorkflowAction(isKnown, identity?.IsBlacklisted ?? false, config)
        };
    }

    private async Task<RecognizedIdentity?> ResolveIdentityAsync(
        string subjectId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            return null;
        }

        var normalizedSubject = subjectId.Trim();

        if (TryParseCanonicalSubject(normalizedSubject, out var personType, out var personId))
        {
            if (personType == "Visitor")
            {
                var visitorById = await _unitOfWork.Visitors.GetByIdAsync(personId, cancellationToken);
                return visitorById != null ? MapVisitorIdentity(visitorById, normalizedSubject) : null;
            }

            var userById = await _unitOfWork.Users.GetByIdAsync(personId, cancellationToken);
            return userById != null ? MapUserIdentity(userById, normalizedSubject) : null;
        }

        var visitor = await _unitOfWork.Visitors.GetByEmailAsync(normalizedSubject, cancellationToken)
            ?? await _unitOfWork.Visitors.GetByFRPersonIdAsync(normalizedSubject, cancellationToken)
            ?? await FindVisitorByLegacyNameSubjectAsync(normalizedSubject, cancellationToken);

        if (visitor != null)
        {
            return MapVisitorIdentity(visitor, normalizedSubject);
        }

        var user = await _unitOfWork.Users.GetByEmailAsync(normalizedSubject, cancellationToken);
        if (user == null && !normalizedSubject.Contains('@', StringComparison.Ordinal))
        {
            user = await _unitOfWork.Users.GetByEmployeeIdAsync(normalizedSubject, cancellationToken)
                ?? (await _unitOfWork.Users.SearchAsync(normalizedSubject, cancellationToken: cancellationToken))
                    .FirstOrDefault();
        }

        return user != null
            ? MapUserIdentity(user, normalizedSubject)
            : null;
    }

    private static bool TryParseCanonicalSubject(string subjectId, out string personType, out int personId)
    {
        personType = string.Empty;
        personId = 0;

        var parts = subjectId.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out personId))
        {
            return false;
        }

        if (parts[0].Equals("visitor", StringComparison.OrdinalIgnoreCase))
        {
            personType = "Visitor";
            return true;
        }

        if (parts[0].Equals("staff", StringComparison.OrdinalIgnoreCase) ||
            parts[0].Equals("user", StringComparison.OrdinalIgnoreCase))
        {
            personType = "Staff";
            return true;
        }

        return false;
    }

    private async Task<Visitor?> FindVisitorByLegacyNameSubjectAsync(
        string subjectId,
        CancellationToken cancellationToken)
    {
        if (subjectId.Contains('@', StringComparison.Ordinal))
        {
            return null;
        }

        var searchTerm = subjectId.Replace("_", " ").Trim();
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return null;
        }

        var result = await _unitOfWork.Visitors.SearchVisitorsAsync(
            searchTerm,
            pageIndex: 0,
            pageSize: 5,
            cancellationToken: cancellationToken);

        return result.Visitors?.FirstOrDefault();
    }

    private RecognizedIdentity MapVisitorIdentity(Visitor visitor, string subjectId)
    {
        return new RecognizedIdentity
        {
            PersonType = "Visitor",
            PersonId = visitor.Id,
            SubjectId = subjectId,
            FullName = visitor.FullName,
            Email = visitor.Email.Value,
            ProfilePhotoUrl = !string.IsNullOrWhiteSpace(visitor.ProfilePhotoPath)
                ? _urlResolver.GetApiUrl($"visitors/{visitor.Id}/photo")
                : null,
            IsVip = visitor.IsVip,
            IsBlacklisted = visitor.IsBlacklisted,
            BlacklistReason = visitor.BlacklistReason
        };
    }

    private RecognizedIdentity MapUserIdentity(User user, string subjectId)
    {
        return new RecognizedIdentity
        {
            PersonType = "Staff",
            PersonId = user.Id,
            SubjectId = subjectId,
            FullName = user.FullName,
            Email = user.Email.Value,
            EmployeeId = user.EmployeeId,
            ProfilePhotoUrl = !string.IsNullOrWhiteSpace(user.ProfilePhotoPath)
                ? _urlResolver.GetAbsoluteUrl(user.ProfilePhotoPath)
                : null
        };
    }

    private static List<CameraFrameFaceResultDto> BuildUnknownFaces(
        IEnumerable<DetectedFace> detectedFaces,
        int maxFaces)
    {
        return detectedFaces
            .OrderByDescending(face => face.Confidence)
            .Take(maxFaces)
            .Select(face => new CameraFrameFaceResultDto
            {
                IsKnown = false,
                PersonType = "Unknown",
                Confidence = face.Confidence,
                BoundingBox = MapBox(face),
                SuggestedAction = "reviewUnknown"
            })
            .ToList();
    }

    private static IEnumerable<CameraFrameFaceResultDto> BuildUnmatchedUnknownFaces(
        IEnumerable<DetectedFace> detectedFaces,
        IReadOnlyCollection<CameraFrameFaceResultDto> knownFaces,
        int remainingSlots)
    {
        if (remainingSlots <= 0)
        {
            return Enumerable.Empty<CameraFrameFaceResultDto>();
        }

        var knownBoxes = knownFaces
            .Where(face => face.BoundingBox != null)
            .Select(face => face.BoundingBox!)
            .ToList();

        return detectedFaces
            .Where(face => knownBoxes.All(box => GetIntersectionOverUnion(MapBox(face), box) < BoundingBoxMatchThreshold))
            .OrderByDescending(face => face.Confidence)
            .Take(remainingSlots)
            .Select(face => new CameraFrameFaceResultDto
            {
                IsKnown = false,
                PersonType = "Unknown",
                Confidence = face.Confidence,
                BoundingBox = MapBox(face),
                SuggestedAction = "reviewUnknown"
            })
            .ToList();
    }

    // Overload for the single-pass Luxand path: preserves bestSimilarity from engine matching.
    private static IEnumerable<CameraFrameFaceResultDto> BuildUnmatchedUnknownFaces(
        IEnumerable<RecognizedFace> recognizedFaces,
        IReadOnlyCollection<CameraFrameFaceResultDto> knownFaces,
        int remainingSlots)
    {
        if (remainingSlots <= 0)
        {
            return Enumerable.Empty<CameraFrameFaceResultDto>();
        }

        var knownBoxes = knownFaces
            .Where(face => face.BoundingBox != null)
            .Select(face => face.BoundingBox!)
            .ToList();

        return recognizedFaces
            .Where(face => face.BoundingBox != null &&
                           knownBoxes.All(box => GetIntersectionOverUnion(MapBox(face.BoundingBox!), box) < BoundingBoxMatchThreshold))
            .OrderByDescending(face => face.BoundingBox!.Confidence)
            .Take(remainingSlots)
            .Select(face => new CameraFrameFaceResultDto
            {
                IsKnown = false,
                PersonType = "Unknown",
                Similarity = face.Similarity > 0 ? face.Similarity : 0,
                Confidence = face.BoundingBox!.Confidence,
                BoundingBox = MapBox(face.BoundingBox!),
                SuggestedAction = "reviewUnknown"
            })
            .ToList();
    }

    private static string GetFaceWorkflowAction(
        bool isKnown,
        bool isBlacklisted,
        Domain.ValueObjects.CameraConfiguration config)
    {
        if (isBlacklisted)
        {
            return "securityAlert";
        }

        if (!isKnown)
        {
            return "reviewUnknown";
        }

        return config.WorkflowMode switch
        {
            CameraWorkflowMode.Disabled => "none",
            CameraWorkflowMode.Automatic when config.CameraRole == CameraRole.Entry => "autoCheckIn",
            CameraWorkflowMode.Automatic when config.CameraRole == CameraRole.Exit => "autoCheckOut",
            CameraWorkflowMode.Manual when config.CameraRole == CameraRole.Entry => "confirmCheckIn",
            CameraWorkflowMode.Manual when config.CameraRole == CameraRole.Exit => "confirmCheckOut",
            _ => "reviewKnown"
        };
    }

    private static string GetFrameWorkflowAction(
        IReadOnlyCollection<CameraFrameFaceResultDto> faces,
        Domain.ValueObjects.CameraConfiguration config)
    {
        if (faces.Any(face => face.IsBlacklisted))
        {
            return "securityAlert";
        }

        if (faces.Any(face => !face.IsKnown))
        {
            return "reviewUnknownFaces";
        }

        if (!faces.Any(face => face.IsKnown))
        {
            return "none";
        }

        return config.WorkflowMode switch
        {
            CameraWorkflowMode.Disabled => "none",
            CameraWorkflowMode.Automatic when config.CameraRole == CameraRole.Entry => "autoCheckIn",
            CameraWorkflowMode.Automatic when config.CameraRole == CameraRole.Exit => "autoCheckOut",
            CameraWorkflowMode.Manual when config.CameraRole == CameraRole.Entry => "confirmCheckIn",
            CameraWorkflowMode.Manual when config.CameraRole == CameraRole.Exit => "confirmCheckOut",
            _ => "reviewKnownFaces"
        };
    }

    private static CameraFrameFaceBoxDto? MapBox(DetectedFace? face)
    {
        if (face == null)
        {
            return null;
        }

        return new CameraFrameFaceBoxDto
        {
            X = face.X,
            Y = face.Y,
            Width = face.Width,
            Height = face.Height,
            Confidence = face.Confidence
        };
    }

    private static void AttachFaceSnapshots(
        IEnumerable<CameraFrameFaceResultDto> faces,
        byte[] frameBytes,
        Domain.ValueObjects.CameraConfiguration config)
    {
        if (!config.SnapshotSavingEnabled)
        {
            return;
        }

        var facesToSnapshot = faces.Where(face =>
            (face.IsKnown && config.StoreKnownFaceSnapshots) ||
            (!face.IsKnown && config.StoreUnknownFaceSnapshots)).ToList();

        if (facesToSnapshot.Count == 0)
        {
            return;
        }

        // Decode the source frame once and crop per face rather than reloading the JPEG each time.
        try
        {
            using var sourceImage = Image.Load(frameBytes);
            foreach (var face in facesToSnapshot)
            {
                var box = face.BoundingBox;
                if (box == null || box.Width < 20 || box.Height < 20) continue;
                face.SnapshotDataUrl = CreateFaceSnapshotDataUrl(sourceImage, box);
            }
        }
        catch
        {
            // Non-critical — inference result is still valid without snapshots.
        }
    }

    private static string? CreateFaceSnapshotDataUrl(Image sourceImage, CameraFrameFaceBoxDto? box)
    {
        try
        {
            var crop = GetSnapshotCrop(sourceImage.Width, sourceImage.Height, box);
            if (crop == null) return null;
            using var cropped = sourceImage.Clone(ctx =>
            {
                ctx.Crop(crop.Value);
                if (crop.Value.Width > 360 || crop.Value.Height > 360)
                {
                    ctx.Resize(new ResizeOptions { Size = new Size(360, 360), Mode = ResizeMode.Max });
                }
            });
            using var output = new MemoryStream();
            cropped.SaveAsJpeg(output, new JpegEncoder { Quality = 82 });
            return $"data:image/jpeg;base64,{Convert.ToBase64String(output.ToArray())}";
        }
        catch
        {
            return null;
        }
    }

    private static Rectangle? GetSnapshotCrop(int imageWidth, int imageHeight, CameraFrameFaceBoxDto? box)
    {
        if (box == null || box.Width < 20 || box.Height < 20)
        {
            return null;
        }

        var paddingX = Math.Max(20, (int)Math.Round(box.Width * 0.35d));
        var paddingY = Math.Max(20, (int)Math.Round(box.Height * 0.45d));
        var left = Math.Clamp(box.X - paddingX, 0, Math.Max(0, imageWidth - 1));
        var top = Math.Clamp(box.Y - paddingY, 0, Math.Max(0, imageHeight - 1));
        var right = Math.Clamp(box.X + box.Width + paddingX, left + 1, imageWidth);
        var bottom = Math.Clamp(box.Y + box.Height + paddingY, top + 1, imageHeight);

        return new Rectangle(left, top, right - left, bottom - top);
    }

    private static double GetIntersectionOverUnion(CameraFrameFaceBoxDto? first, CameraFrameFaceBoxDto? second)
    {
        if (first == null || second == null)
        {
            return 0;
        }

        var xA = Math.Max(first.X, second.X);
        var yA = Math.Max(first.Y, second.Y);
        var xB = Math.Min(first.X + first.Width, second.X + second.Width);
        var yB = Math.Min(first.Y + first.Height, second.Y + second.Height);

        var intersectionWidth = Math.Max(0, xB - xA);
        var intersectionHeight = Math.Max(0, yB - yA);
        var intersectionArea = intersectionWidth * intersectionHeight;
        if (intersectionArea <= 0)
        {
            return 0;
        }

        var firstArea = Math.Max(0, first.Width) * Math.Max(0, first.Height);
        var secondArea = Math.Max(0, second.Width) * Math.Max(0, second.Height);
        var unionArea = firstArea + secondArea - intersectionArea;

        return unionArea <= 0 ? 0 : (double)intersectionArea / unionArea;
    }

    // Sobel-based sharpness score. Returns 0–100 (0=blurry, 100=sharp).
    // Fails open (returns 100) so a broken face crop never silently drops faces.
    private static double ComputeBlurScore(byte[] frameBytes, DetectedFace face)
    {
        try
        {
            using var image = Image.Load<Rgb24>(frameBytes);
            return ComputeBlurScoreFromDecodedImage(image, face);
        }
        catch
        {
            return 100;
        }
    }

    // Hot-path variant — accepts a pre-decoded image so the caller can decode once and score N faces.
    private static double ComputeBlurScoreFromDecodedImage(Image<Rgb24> image, DetectedFace face)
    {
        try
        {
            var x = Math.Max(0, face.X);
            var y = Math.Max(0, face.Y);
            var w = Math.Clamp(face.Width, 1, Math.Max(1, image.Width - x));
            var h = Math.Clamp(face.Height, 1, Math.Max(1, image.Height - y));
            using var roi = image.Clone(ctx => ctx
                .Crop(new Rectangle(x, y, w, h))
                .Grayscale()
                .DetectEdges());
            long edgeSum = 0;
            var pixelCount = 0;
            roi.ProcessPixelRows(accessor =>
            {
                for (var row = 0; row < accessor.Height; row++)
                {
                    var rowSpan = accessor.GetRowSpan(row);
                    for (var col = 0; col < rowSpan.Length; col++)
                    {
                        edgeSum += rowSpan[col].R;
                        pixelCount++;
                    }
                }
            });
            return pixelCount == 0 ? 100 : Math.Clamp(edgeSum / (double)pixelCount / 255.0 * 100.0, 0, 100);
        }
        catch
        {
            return 100;
        }
    }

    private static Image<Rgb24>? TryLoadRgb24(byte[] frameBytes)
    {
        try { return Image.Load<Rgb24>(frameBytes); }
        catch { return null; }
    }

    // Samples ~500 pixels in a grid to compute mean luminance and pixel variance.
    // Returns false for near-black frames (mean < 15/255) or near-uniform frames
    // (variance < 100, i.e. stddev < 10), which are characteristic of RTSP stream
    // artifacts (reconnection, I-frame gaps) and USB camera warm-up frames.
    // Fails open — if the check itself throws, the frame is allowed through.
    private static bool IsFrameUsable(byte[] frameBytes)
    {
        try
        {
            using var image = Image.Load<Rgb24>(frameBytes);
            long sum = 0, sumSq = 0;
            int count = 0;
            var rowStep = Math.Max(1, image.Height / 20);
            var colStep = Math.Max(1, image.Width / 25);

            image.ProcessPixelRows(accessor =>
            {
                for (var row = 0; row < accessor.Height; row += rowStep)
                {
                    var rowSpan = accessor.GetRowSpan(row);
                    for (var col = 0; col < rowSpan.Length; col += colStep)
                    {
                        var lum = (rowSpan[col].R + rowSpan[col].G + rowSpan[col].B) / 3;
                        sum += lum;
                        sumSq += (long)lum * lum;
                        count++;
                    }
                }
            });

            if (count == 0) return true;

            var mean = sum / (double)count;
            if (mean < 15.0) return false;

            var variance = sumSq / (double)count - mean * mean;
            return variance >= 100.0;
        }
        catch
        {
            return true;
        }
    }

    private long GetMaxFrameBytes()
    {
        var configuredBytes = _configuration.GetValue<long?>("CameraFrameProcessing:MaxFrameBytes");
        return configuredBytes.GetValueOrDefault(DefaultMaxFrameBytes);
    }

    private static double GetThreshold(int? percentage)
    {
        var value = percentage.GetValueOrDefault(80);
        return Math.Clamp(value, 0, 100) / 100.0;
    }

    private static bool IsSupportedImage(IFormFile frame)
    {
        if (!string.IsNullOrWhiteSpace(frame.ContentType) &&
            AllowedContentTypes.Contains(frame.ContentType))
        {
            return true;
        }

        var extension = Path.GetExtension(frame.FileName);
        return !string.IsNullOrWhiteSpace(extension) && AllowedExtensions.Contains(extension);
    }

    private static async Task<byte[]> ReadFrameAsync(IFormFile frame, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await frame.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    private sealed class RecognizedIdentity
    {
        public string PersonType { get; init; } = "Unknown";
        public int PersonId { get; init; }
        public string SubjectId { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? Email { get; init; }
        public string? EmployeeId { get; init; }
        public string? ProfilePhotoUrl { get; init; }
        public bool IsVip { get; init; }
        public bool IsBlacklisted { get; init; }
        public string? BlacklistReason { get; init; }
    }

    private sealed record EngineDescriptor(
        string Name,
        IFaceDetectionService Service,
        bool IsFallback);

    private sealed record EngineCallResult<TFace>(
        List<TFace> Faces,
        string? EngineUsed,
        bool FallbackUsed);
}
