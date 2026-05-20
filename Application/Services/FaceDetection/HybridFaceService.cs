namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

/// <summary>
/// Application-wide face service that uses Luxand locally first and CompreFace as fallback.
/// Camera-specific engine selection is handled by CameraFrameRecognitionService.
/// </summary>
public class HybridFaceService : IFaceDetectionService
{
    private readonly LuxandFaceService _luxand;
    private readonly CompreFaceService _compreFace;
    private readonly ILogger<HybridFaceService> _logger;

    public bool IsAvailable => _luxand.IsAvailable || _compreFace.IsAvailable;

    public HybridFaceService(
        LuxandFaceService luxand,
        CompreFaceService compreFace,
        ILogger<HybridFaceService> logger)
    {
        _luxand = luxand;
        _compreFace = compreFace;
        _logger = logger;
    }

    public float MatchTemplates(byte[]? probe, byte[]? stored)
        => _luxand.IsAvailable ? _luxand.MatchTemplates(probe ?? [], stored ?? []) : 0f;

    public async Task<List<DetectedFace>> DetectFacesAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default)
    {
        if (_luxand.IsAvailable)
        {
            return await _luxand.DetectFacesAsync(imageStream, cancellationToken);
        }

        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        return await _compreFace.DetectFacesAsync(imageStream, cancellationToken);
    }

    public async Task<byte[]?> DetectAndCropFaceAsync(
        Stream imageStream,
        int marginPercent = 20,
        CancellationToken cancellationToken = default)
    {
        if (_luxand.IsAvailable)
        {
            return await _luxand.DetectAndCropFaceAsync(imageStream, marginPercent, cancellationToken);
        }

        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        return await _compreFace.DetectAndCropFaceAsync(imageStream, marginPercent, cancellationToken);
    }

    public async Task<FaceRecognitionResult> AddFaceToCollectionAsync(
        byte[] imageBytes,
        string subjectId,
        string? sourcePath = null,
        CancellationToken cancellationToken = default)
    {
        FaceRecognitionResult? luxandResult = null;
        if (_luxand.IsAvailable)
        {
            luxandResult = await _luxand.AddFaceToCollectionAsync(imageBytes, subjectId, sourcePath, cancellationToken);
        }

        FaceRecognitionResult? compreFaceResult = null;
        if (_compreFace.IsAvailable)
        {
            compreFaceResult = await _compreFace.AddFaceToCollectionAsync(imageBytes, subjectId, sourcePath, cancellationToken);
        }

        return luxandResult?.Success == true
            ? luxandResult
            : compreFaceResult ?? luxandResult ?? new FaceRecognitionResult
            {
                Success = false,
                ErrorMessage = "No face recognition engine is available"
            };
    }

    public async Task<bool> RemoveFaceFromCollectionAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        var removed = false;

        if (_luxand.IsAvailable)
        {
            removed |= await _luxand.RemoveFaceFromCollectionAsync(subjectId, cancellationToken);
        }

        if (_compreFace.IsAvailable)
        {
            removed |= await _compreFace.RemoveFaceFromCollectionAsync(subjectId, cancellationToken);
        }

        return removed;
    }

    public async Task<List<RecognizedFace>> RecognizeFacesAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default)
    {
        if (_luxand.IsAvailable)
        {
            try
            {
                var luxandResult = await _luxand.RecognizeFacesAsync(imageStream, cancellationToken);
                return luxandResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Luxand recognition failed; falling back to CompreFace when available.");
            }
        }

        if (!_compreFace.IsAvailable)
        {
            return [];
        }

        // Reset the stream before passing to CompreFace — Luxand may have consumed it.
        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        _logger.LogDebug("Luxand is unavailable or failed; falling back to CompreFace.");
        return await _compreFace.RecognizeFacesAsync(imageStream, cancellationToken);
    }

    public async Task<bool> VerifyFaceAsync(
        byte[] image1,
        byte[] image2,
        CancellationToken cancellationToken = default)
    {
        if (_luxand.IsAvailable)
        {
            return await _luxand.VerifyFaceAsync(image1, image2, cancellationToken);
        }

        return await _compreFace.VerifyFaceAsync(image1, image2, cancellationToken);
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        return _luxand.IsAvailable || await _compreFace.IsServiceAvailableAsync();
    }

    public async Task TrimFacesToMaxAsync(
        string subjectId,
        int maxFaces,
        CancellationToken cancellationToken = default)
    {
        if (_luxand.IsAvailable)
        {
            await _luxand.TrimFacesToMaxAsync(subjectId, maxFaces, cancellationToken);
        }

        if (_compreFace.IsAvailable)
        {
            await _compreFace.TrimFacesToMaxAsync(subjectId, maxFaces, cancellationToken);
        }
    }
}
