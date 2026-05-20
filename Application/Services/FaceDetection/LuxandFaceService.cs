using System.Collections.Concurrent;
using Luxand;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

/// <summary>
/// Local Luxand FaceSDK engine for detection, template extraction, matching, and tracking.
/// </summary>
public class LuxandFaceService : IFaceDetectionService, IFaceTrackerService, IDisposable
{
    private const string EngineName = "Luxand";
    private const long TrackerCameraIndex = 0L;
    private const long TrackerMaxFaces = 64L;

    private readonly LuxandFaceSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LuxandFaceService> _logger;
    private readonly SemaphoreSlim _sdkLock = new(1, 1);
    private readonly SemaphoreSlim _templateLoadLock = new(1, 1);
    private readonly ConcurrentDictionary<int, FaceTemplateCacheEntry> _templates = new();
    private readonly ConcurrentDictionary<int, int> _trackerHandles = new();
    private readonly ConcurrentDictionary<int, int> _trackerMaxFaces = new();

    private bool _initialized;
    private bool _templatesLoaded;
    private int _disposeState;

    public bool IsAvailable => _settings.Enabled && _initialized && _disposeState == 0;
    public string InitializationStatus { get; private set; } = "notStarted";
    public string? InitializationError { get; private set; }
    public int? LastReturnCode { get; private set; }

    public LuxandFaceService(
        IOptions<LuxandFaceSettings> options,
        IServiceScopeFactory scopeFactory,
        ILogger<LuxandFaceService> logger)
    {
        _settings = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        if (_settings.Enabled)
        {
            Initialize();
        }
        else
        {
            InitializationStatus = "disabled";
            InitializationError = "Luxand FaceSDK is disabled in configuration.";
            _logger.LogInformation("Luxand FaceSDK is disabled in configuration.");
        }
    }

    private void Initialize()
    {
        try
        {
            InitializationStatus = "initializing";
            var nativeDllPath = Path.Combine(AppContext.BaseDirectory, "facesdk.dll");
            var projectNativeDllPath = Path.Combine(Directory.GetCurrentDirectory(), "luxand facesdk", "facesdk.dll");

            _logger.LogInformation(
                "Luxand FaceSDK initialization starting. LicenseConfigured={LicenseConfigured}, NativeDllInBaseDirectory={NativeDllInBaseDirectory}, NativeDllInProjectDirectory={NativeDllInProjectDirectory}",
                !string.IsNullOrWhiteSpace(_settings.LicenseKey),
                File.Exists(nativeDllPath),
                File.Exists(projectNativeDllPath));

            if (string.IsNullOrWhiteSpace(_settings.LicenseKey))
            {
                InitializationStatus = "missingLicense";
                InitializationError = "Luxand FaceSDK is enabled but no license key is configured.";
                _logger.LogWarning(InitializationError);
                return;
            }

            var ret = FSDK.ActivateLibrary(_settings.LicenseKey);
            LastReturnCode = ret;
            if (ret != FSDK.FSDKE_OK)
            {
                InitializationStatus = "activationFailed";
                InitializationError = $"Luxand ActivateLibrary failed with code {ret}.";
                _logger.LogError("Luxand ActivateLibrary failed with code {Code}.", ret);
                return;
            }

            ret = FSDK.InitializeLibrary();
            LastReturnCode = ret;
            if (ret != FSDK.FSDKE_OK)
            {
                InitializationStatus = "initializeFailed";
                InitializationError = $"Luxand InitializeLibrary failed with code {ret}.";
                _logger.LogError("Luxand InitializeLibrary failed with code {Code}.", ret);
                return;
            }

            FSDK.SetFaceDetectionThreshold(_settings.DetectionThreshold);
            FSDK.SetFaceDetectionParameters(
                HandleArbitraryRotations: _settings.ArbitraryRotationsEnabled,
                DetermineFaceRotationAngle: false,
                InternalResizeWidth: _settings.InternalResizeWidth);

            _initialized = true;
            InitializationStatus = "available";
            InitializationError = null;
            _logger.LogInformation("Luxand FaceSDK initialized. TemplateSize={TemplateSize}", FSDK.TemplateSize);
        }
        catch (DllNotFoundException ex)
        {
            InitializationStatus = "nativeDllNotFound";
            InitializationError = ex.Message;
            _logger.LogError(ex, "Luxand FaceSDK native DLL could not be loaded.");
        }
        catch (BadImageFormatException ex)
        {
            InitializationStatus = "nativeDllArchitectureMismatch";
            InitializationError = ex.Message;
            _logger.LogError(ex, "Luxand FaceSDK native DLL architecture does not match the running process.");
        }
        catch (Exception ex)
        {
            InitializationStatus = "exception";
            InitializationError = ex.Message;
            _logger.LogError(ex, "Luxand FaceSDK initialization failed.");
        }
    }

    public async Task<List<DetectedFace>> DetectFacesAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return [];
        }

        var bytes = await ReadStreamAsync(imageStream, cancellationToken);
        return await Task.Run(() => DetectFacesFromImage(bytes), cancellationToken);
    }

    public async Task<byte[]?> DetectAndCropFaceAsync(
        Stream imageStream,
        int marginPercent = 20,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return null;
        }

        var bytes = await ReadStreamAsync(imageStream, cancellationToken);
        return await Task.Run(() => CropBestFace(bytes, marginPercent), cancellationToken);
    }

    public async Task<FaceRecognitionResult> AddFaceToCollectionAsync(
        byte[] imageBytes,
        string subjectId,
        string? sourcePath = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return Fail("Luxand FaceSDK is not available.");
        }

        var identity = await ResolveSubjectAsync(subjectId, cancellationToken);
        if (identity == null)
        {
            return Fail($"Subject '{subjectId}' could not be linked to a visitor or staff record.");
        }

        var extraction = await Task.Run(
            () => DetectAndExtractTemplates(imageBytes).OrderByDescending(t => t.QualityScore).FirstOrDefault(),
            cancellationToken);

        if (extraction == null)
        {
            return Fail("No face template could be extracted from the image.");
        }

        var storedTemplate = await StoreTemplateAsync(identity, extraction, sourcePath, cancellationToken);

        return new FaceRecognitionResult
        {
            Success = true,
            SubjectId = identity.SubjectId,
            ImageId = storedTemplate.Id.ToString()
        };
    }

    public async Task<bool> RemoveFaceFromCollectionAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        var identity = await ResolveSubjectAsync(subjectId, cancellationToken);
        if (identity == null)
        {
            return true;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var templates = await unitOfWork.Repository<FaceTemplate>().GetAsync(
            t => t.PersonType == identity.PersonType &&
                 t.PersonId == identity.PersonId &&
                 t.Engine == EngineName,
            cancellationToken);

        foreach (var template in templates)
        {
            template.IsDeleted = true;
            template.IsActive = false;
            template.DeletedOn = DateTime.UtcNow;
            _templates.TryRemove(template.Id, out _);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<RecognizedFace>> RecognizeFacesAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return [];
        }

        await EnsureTemplatesLoadedAsync(cancellationToken);
        // Do NOT return early when _templates is empty. RecognizeFromImage still runs detection
        // and returns all found faces as detected-unknowns (SubjectId = "") so the caller can
        // display them without needing a separate DetectFacesAsync call.

        var bytes = await ReadStreamAsync(imageStream, cancellationToken);
        return await Task.Run(() => RecognizeFromImage(bytes), cancellationToken);
    }

    public async Task<bool> VerifyFaceAsync(
        byte[] image1,
        byte[] image2,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return false;
        }

        return await Task.Run(() =>
        {
            var first = DetectAndExtractTemplates(image1).FirstOrDefault()?.Template;
            var second = DetectAndExtractTemplates(image2).FirstOrDefault()?.Template;
            return first != null && second != null && MatchTemplates(first, second) >= _settings.MatchThreshold;
        }, cancellationToken);
    }

    public Task<bool> IsServiceAvailableAsync() => Task.FromResult(IsAvailable);

    public async Task TrimFacesToMaxAsync(
        string subjectId,
        int maxFaces,
        CancellationToken cancellationToken = default)
    {
        var identity = await ResolveSubjectAsync(subjectId, cancellationToken);
        if (identity == null)
        {
            return;
        }

        await EnforceTemplateLimitAsync(identity, cancellationToken);
    }

    public bool HasCameraTracker(int cameraId) => _trackerHandles.ContainsKey(cameraId);

    public bool CreateCameraTracker(int cameraId, int maxFaces = 64, int trackTimeoutMs = 0, int reIdTimeoutMs = 0)
    {
        if (!IsAvailable)
        {
            return false;
        }

        _sdkLock.Wait();
        try
        {
            if (_disposeState != 0)
            {
                return false;
            }

            if (_trackerHandles.TryRemove(cameraId, out var oldHandle))
            {
                TryFreeTracker(oldHandle);
            }

            var handle = 0;
            if (FSDK.CreateTracker(ref handle) != FSDK.FSDKE_OK)
            {
                _logger.LogWarning("Luxand CreateTracker failed for camera {CameraId}", cameraId);
                return false;
            }

            TrySetTrackerParameter(handle, "RecognitionPrecision", "1");
            TrySetTrackerParameter(handle, "Threshold", _settings.MatchThreshold.ToString("0.00"));

            var effectiveMaxFaces = Math.Max(1, maxFaces);
            TrySetTrackerParameter(handle, "MaxFaces", effectiveMaxFaces.ToString());

            if (trackTimeoutMs > 0)
            {
                TrySetTrackerParameter(handle, "TrackingClearTimeout", trackTimeoutMs.ToString());
            }

            if (reIdTimeoutMs > 0)
            {
                TrySetTrackerParameter(handle, "ReidentificationTimeout", reIdTimeoutMs.ToString());
            }

            _trackerHandles[cameraId] = handle;
            _trackerMaxFaces[cameraId] = effectiveMaxFaces;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Luxand CreateTracker failed for camera {CameraId}", cameraId);
            return false;
        }
        finally
        {
            _sdkLock.Release();
        }
    }

    public void DeleteCameraTracker(int cameraId)
    {
        _trackerMaxFaces.TryRemove(cameraId, out _);

        if (!_trackerHandles.TryRemove(cameraId, out var handle))
        {
            return;
        }

        _sdkLock.Wait();
        try
        {
            TryFreeTracker(handle);
        }
        finally
        {
            _sdkLock.Release();
        }
    }

    public async Task<IReadOnlyList<TrackedFace>> FeedFrameAsync(
        int cameraId,
        byte[] jpegBytes,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || !_trackerHandles.TryGetValue(cameraId, out var trackerHandle))
        {
            return [];
        }

        return await Task.Run(() => FeedFrameInternal(cameraId, trackerHandle, jpegBytes), cancellationToken);
    }

    public float MatchTemplates(byte[]? probe, byte[]? stored)
    {
        if (!IsAvailable || probe == null || probe.Length == 0 || stored == null || stored.Length == 0)
        {
            return 0f;
        }

        try
        {
            // FSDK.MatchFaces takes ref byte[] due to the C binding (unsigned char**) but only
            // reads the template data — it does not reallocate or modify the array contents.
            var similarity = 0f;
            return FSDK.MatchFaces(ref probe, ref stored, ref similarity) == FSDK.FSDKE_OK
                ? similarity
                : 0f;
        }
        catch
        {
            return 0f;
        }
    }

    /// <summary>
    /// Clears the in-memory template cache and reloads all active templates from the database.
    /// Call this when templates are modified outside the current process or after bulk enrollment.
    /// </summary>
    public async Task ReloadTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await _templateLoadLock.WaitAsync(cancellationToken);
        try
        {
            _templates.Clear();
            _templatesLoaded = false;
        }
        finally
        {
            _templateLoadLock.Release();
        }

        await EnsureTemplatesLoadedAsync(cancellationToken);
        _logger.LogInformation("Luxand template cache reloaded. Count={Count}", _templates.Count);
    }

    private async Task EnsureTemplatesLoadedAsync(CancellationToken cancellationToken)
    {
        if (_templatesLoaded)
        {
            return;
        }

        await _templateLoadLock.WaitAsync(cancellationToken);
        try
        {
            if (_templatesLoaded)
            {
                return;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var storedTemplates = await unitOfWork.Repository<FaceTemplate>().GetAsync(
                t => t.Engine == EngineName &&
                     t.IsActive &&
                     !t.IsDeleted &&
                     t.TemplateData.Length > 0,
                cancellationToken);

            foreach (var template in storedTemplates)
            {
                _templates[template.Id] = ToCacheEntry(template);
            }

            _templatesLoaded = true;
            _logger.LogInformation("Loaded {Count} Luxand face template(s) into memory", _templates.Count);
        }
        finally
        {
            _templateLoadLock.Release();
        }
    }

    private async Task<FaceTemplate> StoreTemplateAsync(
        ResolvedFaceSubject identity,
        ExtractedTemplate extraction,
        string? sourcePath,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repository = unitOfWork.Repository<FaceTemplate>();
        var existing = await repository.GetAsync(
            t => t.PersonType == identity.PersonType &&
                 t.PersonId == identity.PersonId &&
                 t.Engine == EngineName &&
                 t.IsActive &&
                 !t.IsDeleted,
            cancellationToken);

        var template = new FaceTemplate
        {
            PersonType = identity.PersonType,
            PersonId = identity.PersonId,
            VisitorId = identity.PersonType == "Visitor" ? identity.PersonId : (int?)null,
            UserId = identity.PersonType == "Staff" ? identity.PersonId : (int?)null,
            SubjectId = identity.SubjectId,
            Engine = EngineName,
            TemplateData = extraction.Template,
            TemplateSize = extraction.Template.Length,
            IsPrimary = existing.All(t => !t.IsPrimary),
            QualityScore = (decimal)extraction.QualityScore,
            SourceImagePath = sourcePath,
            Source = "Enrollment",
            CreatedOn = DateTime.UtcNow,
            IsActive = true
        };

        await repository.AddAsync(template, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        _templates[template.Id] = ToCacheEntry(template);

        await EnforceTemplateLimitAsync(identity, cancellationToken);
        return template;
    }

    private async Task EnforceTemplateLimitAsync(
        ResolvedFaceSubject identity,
        CancellationToken cancellationToken)
    {
        var maxAdditional = Math.Max(0, _settings.MaxAdditionalTemplatesPerIdentity);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var repository = unitOfWork.Repository<FaceTemplate>();
        var templates = await repository.GetAsync(
            t => t.PersonType == identity.PersonType &&
                 t.PersonId == identity.PersonId &&
                 t.Engine == EngineName &&
                 t.IsActive &&
                 !t.IsDeleted,
            cancellationToken);

        var removable = templates
            .Where(t => !t.IsPrimary)
            .OrderBy(t => t.CreatedOn)
            .ToList();

        var excess = removable.Count - maxAdditional;
        if (excess <= 0)
        {
            return;
        }

        foreach (var template in removable.Take(excess))
        {
            template.IsDeleted = true;
            template.IsActive = false;
            template.DeletedOn = DateTime.UtcNow;
            _templates.TryRemove(template.Id, out _);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<ResolvedFaceSubject?> ResolveSubjectAsync(
        string subjectId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            return null;
        }

        var normalized = subjectId.Trim();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (TryParseCanonicalSubject(normalized, out var canonical))
        {
            return canonical;
        }

        if (int.TryParse(normalized, out var visitorId))
        {
            var visitorById = await unitOfWork.Visitors.GetByIdAsync(visitorId, cancellationToken);
            if (visitorById != null)
            {
                return ResolvedFaceSubject.Visitor(visitorById.Id);
            }
        }

        var visitor = await unitOfWork.Visitors.GetByEmailAsync(normalized, cancellationToken)
            ?? await unitOfWork.Visitors.GetByFRPersonIdAsync(normalized, cancellationToken);

        if (visitor == null && !normalized.Contains('@', StringComparison.Ordinal))
        {
            var searchTerm = normalized.Replace("_", " ").Trim();
            visitor = (await unitOfWork.Visitors.SearchVisitorsAsync(
                searchTerm,
                pageIndex: 0,
                pageSize: 1,
                cancellationToken: cancellationToken)).Visitors?.FirstOrDefault();
        }

        if (visitor != null)
        {
            return ResolvedFaceSubject.Visitor(visitor.Id);
        }

        var user = await unitOfWork.Users.GetByEmailAsync(normalized, cancellationToken);
        if (user == null && !normalized.Contains('@', StringComparison.Ordinal))
        {
            user = await unitOfWork.Users.GetByEmployeeIdAsync(normalized, cancellationToken)
                ?? (await unitOfWork.Users.SearchAsync(normalized, cancellationToken: cancellationToken)).FirstOrDefault();
        }

        return user != null ? ResolvedFaceSubject.Staff(user.Id) : null;
    }

    private static bool TryParseCanonicalSubject(string subjectId, out ResolvedFaceSubject subject)
    {
        subject = default!;
        var parts = subjectId.Split(':', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var personId))
        {
            return false;
        }

        if (parts[0].Equals("visitor", StringComparison.OrdinalIgnoreCase))
        {
            subject = ResolvedFaceSubject.Visitor(personId);
            return true;
        }

        if (parts[0].Equals("staff", StringComparison.OrdinalIgnoreCase) ||
            parts[0].Equals("user", StringComparison.OrdinalIgnoreCase))
        {
            subject = ResolvedFaceSubject.Staff(personId);
            return true;
        }

        return false;
    }

    private List<RecognizedFace> RecognizeFromImage(byte[] imageBytes)
    {
        // Single SDK image load: detect + extract templates + match in one pass.
        var extractedTemplates = DetectAndExtractTemplates(imageBytes);
        if (extractedTemplates.Count == 0)
        {
            return [];
        }

        var recognized = new List<RecognizedFace>();
        foreach (var extraction in extractedTemplates)
        {
            if (extraction.Template.Length == 0)
            {
                // Template extraction failed (quality=0 is already set by DetectAndExtractTemplates).
                // Include as detected-unknown so callers can display them without a separate detection pass.
                recognized.Add(new RecognizedFace { BoundingBox = extraction.Face, Similarity = 0, Confidence = 0 });
                continue;
            }

            // Linear scan — faster than LINQ+Select because we avoid per-iteration object allocation.
            // FSDK.MatchFaces reads templates without modifying them; no cloning needed.
            FaceTemplateCacheEntry? bestEntry = null;
            var bestSimilarity = 0f;
            foreach (var stored in _templates.Values)
            {
                var sim = MatchTemplates(extraction.Template, stored.TemplateData);
                if (sim > bestSimilarity)
                {
                    bestSimilarity = sim;
                    bestEntry = stored;
                }
            }

            if (bestEntry == null || bestSimilarity < _settings.MatchThreshold)
            {
                // Detected but not matched to any identity.
                // Returned with empty SubjectId so callers can show them as unknown without a second detection pass.
                recognized.Add(new RecognizedFace
                {
                    BoundingBox = extraction.Face,
                    Similarity = bestSimilarity,
                    Confidence = 0,
                    TemplateBytes = extraction.Template
                });
            }
            else
            {
                recognized.Add(new RecognizedFace
                {
                    SubjectId = bestEntry.SubjectId,
                    Similarity = bestSimilarity,
                    Confidence = bestSimilarity,
                    BoundingBox = extraction.Face,
                    TemplateBytes = extraction.Template
                });
            }
        }

        return recognized
            .OrderByDescending(face => face.Similarity)
            .ToList();
    }

    private List<DetectedFace> DetectFacesFromImage(byte[] imageBytes)
    {
        return ExtractFacePositions(imageBytes)
            .Select(e =>
            {
                e.Face.QualityScore = e.QualityScore;
                return e.Face;
            })
            .ToList();
    }

    private byte[]? CropBestFace(byte[] imageBytes, int marginPercent)
    {
        var extraction = ExtractFacePositions(imageBytes)
            .OrderByDescending(e => e.QualityScore)
            .FirstOrDefault();

        if (extraction == null)
        {
            return null;
        }

        try
        {
            using var image = Image.Load(imageBytes);
            var marginX = (int)(extraction.Face.Width * marginPercent / 100.0);
            var marginY = (int)(extraction.Face.Height * marginPercent / 100.0);
            var x = Math.Max(0, extraction.Face.X - marginX);
            var y = Math.Max(0, extraction.Face.Y - marginY);
            var width = Math.Min(image.Width - x, extraction.Face.Width + marginX * 2);
            var height = Math.Min(image.Height - y, extraction.Face.Height + marginY * 2);

            if (width <= 0 || height <= 0)
            {
                return null;
            }

            image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, width, height)));
            using var output = new MemoryStream();
            image.SaveAsJpeg(output);
            return output.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Luxand face crop failed.");
            return null;
        }
    }

    /// <summary>
    /// Detects faces and extracts face templates in a single SDK image load.
    /// Avoids the previous pattern of loading the image twice (once for detection, once for template extraction).
    /// </summary>
    private List<ExtractedTemplate> DetectAndExtractTemplates(byte[] imageBytes)
    {
        if (_disposeState != 0)
        {
            return [];
        }

        var result = new List<ExtractedTemplate>();
        var image = -1;

        // FSDK detection and template functions are thread-safe per SDK documentation;
        // each call works exclusively with its own local HImage handle.
        try
        {
            image = LoadImageToFsdk(imageBytes);
            if (image < 0)
            {
                _logger.LogDebug(
                    "DetectAndExtractTemplates: LoadImageToFsdk returned -1. BufferLen={BufferLen}",
                    imageBytes.Length);
                return result;
            }

            var imageWidth = 0;
            var imageHeight = 0;
            FSDK.GetImageWidth(image, ref imageWidth);
            FSDK.GetImageHeight(image, ref imageHeight);

            var count = 0;
            FSDK.TFacePosition[]? positions = null;
            var ret = FSDK.DetectMultipleFaces(image, ref count, out positions, 256 * FSDK.sizeofTFacePosition);

            _logger.LogDebug(
                "Luxand DetectMultipleFaces. ImageSize={Width}x{Height}, ReturnCode={ReturnCode}, FaceCount={Count}",
                imageWidth, imageHeight, ret, count);

            if (ret != FSDK.FSDKE_OK)
            {
                _logger.LogWarning(
                    "Luxand DetectMultipleFaces returned error. ReturnCode={ReturnCode}, ImageSize={Width}x{Height}",
                    ret, imageWidth, imageHeight);
                return result;
            }

            if (count <= 0 || positions == null)
            {
                return result;
            }

            for (var i = 0; i < count && i < positions.Length; i++)
            {
                var position = positions[i];
                var half = position.w / 2;
                var qualityScore = 0.0;
                var face = new DetectedFace
                {
                    X = Math.Max(0, position.xc - half),
                    Y = Math.Max(0, position.yc - half),
                    Width = position.w,
                    Height = position.w,
                    Confidence = 1.0,
                    Roll = (float?)position.angle
                };
                face.QualityScore = qualityScore = CalculateQualityScore(face, imageWidth, imageHeight);

                // Extract the face template from this region while the image is already loaded.
                byte[]? template = null;
                var pos = position;
                FSDK.GetFaceTemplateInRegion(image, ref pos, out template);

                var hasTemplate = template is { Length: > 0 };
                // A face with no extractable template cannot be matched — zero out its quality
                // so it is dropped by the quality filter rather than silently failing later.
                if (!hasTemplate)
                {
                    qualityScore = 0;
                    face.QualityScore = 0;
                }

                // Use quality score as detection confidence — Luxand doesn't expose a raw
                // detection probability, so quality is the best available proxy.
                face.Confidence = qualityScore / 100.0;

                result.Add(new ExtractedTemplate(
                    face,
                    position,
                    hasTemplate ? template! : Array.Empty<byte>(),
                    qualityScore));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Luxand detect-and-extract failed.");
        }
        finally
        {
            if (image >= 0)
            {
                FSDK.FreeImage(image);
            }
        }

        return result;
    }

    /// <summary>
    /// Detects face positions only (no template extraction). Used by crop and detection-only paths.
    /// </summary>
    private List<ExtractedTemplate> ExtractFacePositions(byte[] imageBytes)
    {
        if (_disposeState != 0)
        {
            return [];
        }

        var result = new List<ExtractedTemplate>();
        var image = -1;

        try
        {
            image = LoadImageToFsdk(imageBytes);
            if (image < 0)
            {
                _logger.LogDebug(
                    "ExtractFacePositions: LoadImageToFsdk returned -1. BufferLen={BufferLen}",
                    imageBytes.Length);
                return result;
            }

            var imageWidth = 0;
            var imageHeight = 0;
            FSDK.GetImageWidth(image, ref imageWidth);
            FSDK.GetImageHeight(image, ref imageHeight);

            var count = 0;
            FSDK.TFacePosition[]? positions = null;
            var ret = FSDK.DetectMultipleFaces(
                image,
                ref count,
                out positions,
                256 * FSDK.sizeofTFacePosition);

            _logger.LogDebug(
                "Luxand ExtractFacePositions. ImageSize={Width}x{Height}, ReturnCode={ReturnCode}, FaceCount={Count}",
                imageWidth, imageHeight, ret, count);

            if (ret != FSDK.FSDKE_OK)
            {
                _logger.LogWarning(
                    "Luxand ExtractFacePositions DetectMultipleFaces returned error. ReturnCode={ReturnCode}, ImageSize={Width}x{Height}",
                    ret, imageWidth, imageHeight);
                return result;
            }

            if (count <= 0 || positions == null)
            {
                return result;
            }

            for (var i = 0; i < count && i < positions.Length; i++)
            {
                var position = positions[i];
                var half = position.w / 2;
                var face = new DetectedFace
                {
                    X = Math.Max(0, position.xc - half),
                    Y = Math.Max(0, position.yc - half),
                    Width = position.w,
                    Height = position.w,
                };
                var quality = CalculateQualityScore(face, imageWidth, imageHeight);
                face.QualityScore = quality;
                face.Confidence = quality / 100.0;

                result.Add(new ExtractedTemplate(
                    face,
                    position,
                    Array.Empty<byte>(),
                    quality));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Luxand face detection failed.");
        }
        finally
        {
            if (image >= 0)
            {
                FSDK.FreeImage(image);
            }
        }

        return result;
    }

    private IReadOnlyList<TrackedFace> FeedFrameInternal(int cameraId, int trackerHandle, byte[] imageBytes)
    {
        var result = new List<TrackedFace>();
        var image = -1;

        _sdkLock.Wait();
        try
        {
            if (_disposeState != 0)
            {
                return result;
            }

            image = LoadImageToFsdk(imageBytes);
            if (image < 0)
            {
                return result;
            }

            long faceCount = 0;
            long[]? ids = null;
            var bufferSize = (long)_trackerMaxFaces.GetValueOrDefault(cameraId, (int)TrackerMaxFaces) * 8;
            var ret = FSDK.FeedFrame(trackerHandle, TrackerCameraIndex, image, ref faceCount, out ids, bufferSize);
            if (ret != FSDK.FSDKE_OK || faceCount <= 0 || ids == null)
            {
                return result;
            }

            for (var i = 0; i < faceCount && i < ids.Length; i++)
            {
                var faceId = ids[i];
                var position = new FSDK.TFacePosition();
                if (FSDK.GetTrackerFacePosition(trackerHandle, TrackerCameraIndex, faceId, ref position) != FSDK.FSDKE_OK)
                {
                    continue;
                }

                byte[]? template = null;
                FSDK.TPoint[]? eyes = null;
                if (FSDK.GetTrackerEyes(trackerHandle, TrackerCameraIndex, faceId, out eyes) == FSDK.FSDKE_OK &&
                    eyes is { Length: >= 2 })
                {
                    FSDK.GetFaceTemplateUsingEyes(image, ref eyes, out template);
                }

                var half = position.w / 2;
                result.Add(new TrackedFace
                {
                    FaceId = faceId,
                    X = Math.Max(0, position.xc - half),
                    Y = Math.Max(0, position.yc - half),
                    Width = position.w,
                    Height = position.w,
                    Template = template is { Length: > 0 } ? template : null
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Luxand FeedFrame failed for camera {CameraId}", cameraId);
        }
        finally
        {
            if (image >= 0)
            {
                FSDK.FreeImage(image);
            }

            _sdkLock.Release();
        }

        return result;
    }

    private int LoadImageToFsdk(byte[] imageBytes)
    {
        try
        {
            // FSDK_IMAGE_COLOR_24BIT is 24-bit BGR (Windows BITMAP convention).
            // ImageSharp Bgr24.CopyPixelDataTo produces B,G,R packed bytes — the correct order.
            var srcBytes = IsJpeg(imageBytes) ? imageBytes : ConvertToJpeg(imageBytes);
            using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Bgr24>(srcBytes);
            var pixels = new byte[img.Width * img.Height * 3];
            img.CopyPixelDataTo(pixels);
            var image = 0;
            var ret = FSDK.LoadImageFromBuffer(ref image, pixels, img.Width, img.Height, img.Width * 3, FSDK.FSDK_IMAGEMODE.FSDK_IMAGE_COLOR_24BIT);
            if (ret != FSDK.FSDKE_OK)
            {
                _logger.LogWarning(
                    "Luxand LoadImageFromBuffer failed. ReturnCode={ReturnCode}, ImageSize={Width}x{Height}, BufferLen={BufferLen}",
                    ret, img.Width, img.Height, pixels.Length);
                return -1;
            }

            _logger.LogDebug(
                "Luxand image loaded. Width={Width}, Height={Height}, Stride={Stride}, BufferLen={BufferLen}",
                img.Width, img.Height, img.Width * 3, pixels.Length);

            TryDumpDebugFrame(imageBytes, img.Width, img.Height);
            return image;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Luxand LoadImageToFsdk failed. BufferLen={BufferLen}", imageBytes.Length);
            return -1;
        }
    }

    private void TryDumpDebugFrame(byte[] jpegBytes, int width, int height)
    {
        if (!_settings.DebugFrameDumpEnabled || string.IsNullOrWhiteSpace(_settings.DebugFrameDumpPath))
            return;

        try
        {
            var dir = Path.GetFullPath(_settings.DebugFrameDumpPath);
            Directory.CreateDirectory(dir);
            var name = $"frame-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{width}x{height}.jpg";
            File.WriteAllBytes(Path.Combine(dir, name), jpegBytes);
            _logger.LogDebug("Debug frame dumped to {Path}", Path.Combine(dir, name));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Debug frame dump failed");
        }
    }

    private static double CalculateQualityScore(DetectedFace face, int imageWidth, int imageHeight)
    {
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return 0;
        }

        var minDimension = Math.Max(1, Math.Min(imageWidth, imageHeight));
        var sizeRatio = Math.Clamp(face.Width / (double)minDimension, 0, 1);
        var sizeScore = Math.Clamp(sizeRatio / 0.35, 0, 1) * 70;

        var centerX = face.X + face.Width / 2d;
        var centerY = face.Y + face.Height / 2d;
        var offsetX = Math.Abs(centerX - imageWidth / 2d) / Math.Max(1, imageWidth / 2d);
        var offsetY = Math.Abs(centerY - imageHeight / 2d) / Math.Max(1, imageHeight / 2d);
        var centerScore = (1 - Math.Clamp((offsetX + offsetY) / 2d, 0, 1)) * 30;

        return Math.Round(sizeScore + centerScore, 3);
    }

    private static bool IsJpeg(byte[] bytes)
    {
        return bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
    }

    private static byte[] ConvertToJpeg(byte[] imageBytes)
    {
        using var image = Image.Load(imageBytes);
        using var output = new MemoryStream();
        image.SaveAsJpeg(output);
        return output.ToArray();
    }

    private static async Task<byte[]> ReadStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private static FaceRecognitionResult Fail(string message)
    {
        return new FaceRecognitionResult
        {
            Success = false,
            ErrorMessage = message
        };
    }

    private static FaceTemplateCacheEntry ToCacheEntry(FaceTemplate template)
    {
        return new FaceTemplateCacheEntry(
            template.Id,
            template.PersonType,
            template.PersonId,
            template.SubjectId,
            template.TemplateData);
    }

    private static void TrySetTrackerParameter(int handle, string key, string value)
    {
        try
        {
            FSDK.SetTrackerParameter(handle, key, value);
        }
        catch
        {
            // Older SDK builds may not support all tracker parameters.
        }
    }

    private static void TryFreeTracker(int handle)
    {
        try
        {
            FSDK.FreeTracker(handle);
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        _initialized = false;
        var lockAcquired = _sdkLock.Wait(TimeSpan.FromSeconds(5));
        try
        {
            foreach (var handle in _trackerHandles.Values)
            {
                TryFreeTracker(handle);
            }

            _trackerHandles.Clear();
            _logger.LogInformation("Luxand FaceSDK disposed. FinalizeLibrary intentionally skipped for process stability.");
        }
        finally
        {
            if (lockAcquired)
            {
                _sdkLock.Release();
            }

            _sdkLock.Dispose();
            _templateLoadLock.Dispose();
        }
    }

    private sealed record FaceTemplateCacheEntry(
        int Id,
        string PersonType,
        int PersonId,
        string SubjectId,
        byte[] TemplateData);

    private sealed record ResolvedFaceSubject(string PersonType, int PersonId, string SubjectId)
    {
        public static ResolvedFaceSubject Visitor(int visitorId) => new("Visitor", visitorId, $"visitor:{visitorId}");
        public static ResolvedFaceSubject Staff(int userId) => new("Staff", userId, $"staff:{userId}");
    }

    private sealed record ExtractedTemplate(
        DetectedFace Face,
        FSDK.TFacePosition Position,
        byte[] Template,
        double QualityScore);
}
