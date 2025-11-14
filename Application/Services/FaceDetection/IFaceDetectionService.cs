namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

/// <summary>
/// Service for face detection, recognition, and verification using CompreFace
/// </summary>
public interface IFaceDetectionService
{
    /// <summary>
    /// Detects faces in an image and returns the detected face regions
    /// </summary>
    /// <param name="imageStream">Image stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of detected faces with bounding boxes</returns>
    Task<List<DetectedFace>> DetectFacesAsync(Stream imageStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects a face and crops the image to show only the face with margin
    /// </summary>
    /// <param name="imageStream">Original image stream</param>
    /// <param name="marginPercent">Percentage of margin to add around the face (default 20%)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cropped image as byte array, or null if no face detected</returns>
    Task<byte[]?> DetectAndCropFaceAsync(Stream imageStream, int marginPercent = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a face to the CompreFace recognition collection for later identification
    /// </summary>
    /// <param name="imageBytes">Image bytes</param>
    /// <param name="subjectId">Subject identifier (e.g., visitor ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of adding face to collection</returns>
    Task<FaceRecognitionResult> AddFaceToCollectionAsync(byte[] imageBytes, string subjectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all face examples for a subject from the recognition collection
    /// </summary>
    /// <param name="subjectId">Subject identifier to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> RemoveFaceFromCollectionAsync(string subjectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recognizes faces in an image by matching against the face collection
    /// </summary>
    /// <param name="imageStream">Image stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recognized faces with subject IDs and similarity scores</returns>
    Task<List<RecognizedFace>> RecognizeFacesAsync(Stream imageStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies if two face images belong to the same person
    /// </summary>
    /// <param name="image1">First image bytes</param>
    /// <param name="image2">Second image bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if faces match with sufficient similarity</returns>
    Task<bool> VerifyFaceAsync(byte[] image1, byte[] image2, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies if CompreFace service is available and configured
    /// </summary>
    /// <returns>True if service is available</returns>
    Task<bool> IsServiceAvailableAsync();
}

/// <summary>
/// Represents a detected face with bounding box coordinates
/// </summary>
public class DetectedFace
{
    /// <summary>
    /// X coordinate of top-left corner
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Y coordinate of top-left corner
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Width of face region
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height of face region
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Detection confidence score (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Age estimation (optional, if enabled in CompreFace)
    /// </summary>
    public int? Age { get; set; }

    /// <summary>
    /// Gender prediction (optional, if enabled in CompreFace)
    /// </summary>
    public string? Gender { get; set; }
}

/// <summary>
/// Result of adding a face to the recognition collection
/// </summary>
public class FaceRecognitionResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Subject identifier
    /// </summary>
    public string? SubjectId { get; set; }

    /// <summary>
    /// Image ID assigned by CompreFace
    /// </summary>
    public string? ImageId { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents a recognized face matched against the face collection
/// </summary>
public class RecognizedFace
{
    /// <summary>
    /// Subject identifier matched from the collection
    /// </summary>
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// Similarity score (0-1) between detected face and stored subject
    /// </summary>
    public double Similarity { get; set; }

    /// <summary>
    /// Detection confidence score (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Bounding box of the detected face
    /// </summary>
    public DetectedFace? BoundingBox { get; set; }
}

/// <summary>
/// Configuration settings for CompreFace
/// </summary>
public class CompreFaceSettings
{
    /// <summary>
    /// Whether CompreFace integration is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// CompreFace server base URL (e.g., http://localhost:8000)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key for CompreFace Detection Service
    /// </summary>
    public string DetectionApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API key for CompreFace Recognition Service
    /// </summary>
    public string RecognitionApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API key for CompreFace Verification Service
    /// </summary>
    public string VerificationApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Default margin percentage around detected face (default: 20%)
    /// </summary>
    public int DefaultMarginPercent { get; set; } = 20;

    /// <summary>
    /// Minimum confidence score to accept face detection (0-1, default: 0.8)
    /// </summary>
    public double MinimumConfidence { get; set; } = 0.8;

    /// <summary>
    /// Minimum similarity score to accept face recognition (0-1, default: 0.85)
    /// </summary>
    public double MinimumSimilarity { get; set; } = 0.85;

    /// <summary>
    /// Maximum number of faces to detect in an image (default: 1)
    /// </summary>
    public int MaxFacesDetect { get; set; } = 1;
}
