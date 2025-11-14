namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// Result of photo upload operation
/// </summary>
public class PhotoUploadResult
{
    /// <summary>
    /// URL of the uploaded photo
    /// </summary>
    public string PhotoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether a face was detected in the photo
    /// </summary>
    public bool FaceDetected { get; set; }

    /// <summary>
    /// Whether face recognition is available for this photo
    /// </summary>
    public bool FaceRecognitionEnabled { get; set; }

    /// <summary>
    /// Warning message if any (e.g., no face detected)
    /// </summary>
    public string? WarningMessage { get; set; }

    /// <summary>
    /// Type of warning (NoFace, ServiceError, PartialSuccess)
    /// </summary>
    public PhotoUploadWarningType? WarningType { get; set; }

    /// <summary>
    /// Indicates if the operation should be retried
    /// </summary>
    public bool ShouldRetry { get; set; }
}

/// <summary>
/// Types of warnings that can occur during photo upload
/// </summary>
public enum PhotoUploadWarningType
{
    /// <summary>
    /// No face was detected in the uploaded image
    /// </summary>
    NoFace,

    /// <summary>
    /// Face detection service encountered an error
    /// </summary>
    ServiceError,

    /// <summary>
    /// Photo was saved but face recognition failed
    /// </summary>
    PartialSuccess,

    /// <summary>
    /// Face detection service is unavailable
    /// </summary>
    ServiceUnavailable
}
